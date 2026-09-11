-- The same warehouse/order schema as dev-warehouse.sql, in Oracle's dialect.
--
-- Deliberately the same shape, so a statement written against one engine is recognisably the same
-- job on another and the differences are the ones that are real: a parameter is :name, a sequence
-- is an object, a comment is its own statement, and a procedure hands back rows through an
-- explicit REF CURSOR rather than by SELECTing.

create table customers (
    id           number generated always as identity primary key,
    code         varchar2(20)  not null unique,
    name         varchar2(120) not null,
    country      char(2)       not null,
    credit_limit number(12,2)  default 0 not null
);

comment on table customers is 'Trading partners we ship to';

create table orders (
    id           number generated always as identity primary key,
    order_no     varchar2(30)  not null unique,
    customer_id  number        not null references customers (id),
    status       varchar2(20)  default 'NEW' not null,
    total        number(12,2)  not null,
    currency     char(3)       default 'USD' not null,
    placed_on    timestamp with time zone default systimestamp not null,
    updated_on   timestamp with time zone default systimestamp not null
);

comment on table orders is 'Sales orders, the table Bitween writes into';

create index ix_orders_status on orders (status);
create index ix_orders_updated on orders (updated_on);

create table order_lines (
    id          number generated always as identity primary key,
    order_id    number       not null references orders (id) on delete cascade,
    sku         varchar2(40) not null,
    quantity    number       not null,
    unit_price  number(12,2) not null
);

-- The outbox an integration polls: rows appear here, Bitween drains them.
create table shipment_outbox (
    id           number generated always as identity primary key,
    order_no     varchar2(30) not null,
    payload      clob         not null,
    created_on   timestamp with time zone default systimestamp not null,
    processed    number(1)    default 0 not null,
    processed_on timestamp with time zone
);

comment on table shipment_outbox is 'Shipment notifications waiting to be picked up';

create index ix_outbox_pending on shipment_outbox (processed, id);

create sequence shipment_ref_seq start with 5000 increment by 1;

insert into customers (code, name, country, credit_limit) values ('ACME',    'Acme Trading Co',    'JO', 50000);
insert into customers (code, name, country, credit_limit) values ('GLOBEX',  'Globex Corporation', 'AE', 120000);
insert into customers (code, name, country, credit_limit) values ('INITECH', 'Initech LLC',        'US', 25000);
insert into customers (code, name, country, credit_limit) values ('UMBRA',   'Umbrella Logistics', 'DE', 80000);

insert into orders (order_no, customer_id, status, total, currency, placed_on)
select 'SO-' || lpad(level, 5, '0'),
       1 + mod(level, 4),
       decode(mod(level, 4), 0, 'NEW', 1, 'RELEASED', 2, 'SHIPPED', 'INVOICED'),
       round(dbms_random.value(100, 4100), 2),
       decode(mod(level, 3), 0, 'USD', 1, 'EUR', 'JOD'),
       systimestamp - numtodsinterval(level, 'HOUR')
  from dual connect by level <= 60;

insert into order_lines (order_id, sku, quantity, unit_price)
select o.id,
       'SKU-' || lpad(mod(o.id * 7 + l.n, 200), 4, '0'),
       1 + mod(o.id + l.n, 9),
       round(dbms_random.value(5, 305), 2)
  from orders o
 cross join (select level as n from dual connect by level <= 3) l;

insert into shipment_outbox (order_no, payload)
select o.order_no,
       json_object('orderNo' value o.order_no, 'status' value o.status,
                   'total' value o.total, 'currency' value o.currency)
  from orders o
 where o.status in ('SHIPPED', 'INVOICED');

commit;

-- A REF CURSOR procedure: Oracle's way of handing rows back from a CALL, and the reason the
-- adapter's capability list says procedureResultSets is true here and false on PostgreSQL.
create or replace procedure orders_for_customer(p_code in varchar2, p_rows out sys_refcursor)
as
begin
    open p_rows for
        select o.order_no, o.status, o.total, o.placed_on
          from orders o join customers c on c.id = o.customer_id
         where c.code = p_code
         order by o.placed_on desc;
end;
/

-- A real procedure: called, returns nothing.
create or replace procedure release_order(p_order_no in varchar2)
as
begin
    update orders set status = 'RELEASED', updated_on = systimestamp where order_no = p_order_no;
    commit;
end;
/

-- And one that answers through an OUT parameter rather than a cursor.
create or replace procedure count_open_orders(p_code in varchar2, p_total out number)
as
begin
    select count(*) into p_total
      from orders o join customers c on c.id = o.customer_id
     where c.code = p_code and o.status in ('NEW', 'RELEASED');
end;
/
