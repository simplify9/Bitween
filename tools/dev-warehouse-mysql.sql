-- The same warehouse/order schema as dev-warehouse.sql, in MySQL's dialect.
--
-- Deliberately the same shape, so a statement written against one engine is recognisably the same
-- job on another and the differences are the ones that are real: no sequences, a procedure that
-- returns rows by SELECTing, and `on duplicate key update` where PostgreSQL would write `merge`.

create table customers (
    id           int auto_increment primary key,
    code         varchar(20)   not null unique,
    name         varchar(120)  not null,
    country      char(2)       not null,
    credit_limit decimal(12,2) not null default 0
) comment 'Trading partners we ship to';

create table orders (
    id           int auto_increment primary key,
    order_no     varchar(30)   not null unique,
    customer_id  int           not null,
    status       varchar(20)   not null default 'NEW',
    total        decimal(12,2) not null,
    currency     char(3)       not null default 'USD',
    placed_on    timestamp     not null default current_timestamp,
    updated_on   timestamp     not null default current_timestamp on update current_timestamp,
    constraint fk_orders_customer foreign key (customer_id) references customers (id)
) comment 'Sales orders, the table Bitween writes into';

create index ix_orders_status on orders (status);
create index ix_orders_updated on orders (updated_on);

create table order_lines (
    id          int auto_increment primary key,
    order_id    int           not null,
    sku         varchar(40)   not null,
    quantity    int           not null,
    unit_price  decimal(12,2) not null,
    constraint fk_lines_order foreign key (order_id) references orders (id) on delete cascade
);

-- The outbox an integration polls: rows appear here, Bitween drains them.
create table shipment_outbox (
    id           int auto_increment primary key,
    order_no     varchar(30) not null,
    payload      json        not null,
    created_on   timestamp   not null default current_timestamp,
    processed    tinyint(1)  not null default 0,
    processed_on timestamp   null
) comment 'Shipment notifications waiting to be picked up';

create index ix_outbox_pending on shipment_outbox (processed, id);

-- No sequence: MySQL has AUTO_INCREMENT, which belongs to a column rather than being an object of
-- its own. A statement that needs the next reference reads it from a counter table instead, which
-- is the usual stand-in and is why the adapter's supported-object list leaves sequences out.
create table shipment_ref (
    name      varchar(40) primary key,
    next_value bigint not null
);
insert into shipment_ref (name, next_value) values ('shipment', 5000);

delimiter //

-- Returns rows by SELECTing. This is the shape PostgreSQL needs a set-returning function for and
-- Oracle needs an explicit REF CURSOR for; here it is just a procedure.
create procedure orders_for_customer(in p_code varchar(20))
begin
    select o.order_no, o.status, o.total, o.placed_on
      from orders o join customers c on c.id = o.customer_id
     where c.code = p_code
     order by o.placed_on desc;
end //

-- A real procedure: called, returns nothing.
create procedure release_order(in p_order_no varchar(30))
begin
    update orders set status = 'RELEASED' where order_no = p_order_no;
end //

-- And one that answers through an OUT parameter rather than a result set.
create procedure count_open_orders(in p_code varchar(20), out p_total int)
begin
    select count(*) into p_total
      from orders o join customers c on c.id = o.customer_id
     where c.code = p_code and o.status in ('NEW', 'RELEASED');
end //

delimiter ;

insert into customers (code, name, country, credit_limit) values
    ('ACME',    'Acme Trading Co',    'JO', 50000),
    ('GLOBEX',  'Globex Corporation', 'AE', 120000),
    ('INITECH', 'Initech LLC',        'US', 25000),
    ('UMBRA',   'Umbrella Logistics', 'DE', 80000);

-- No generate_series in MySQL, so the rows come from a recursive CTE. cte_max_recursion_depth
-- defaults to 1000, which is comfortably above the 60 wanted here.
insert into orders (order_no, customer_id, status, total, currency, placed_on)
with recursive g (n) as (select 1 union all select n + 1 from g where n < 60)
select concat('SO-', lpad(n, 5, '0')),
       1 + (n % 4),
       elt(1 + (n % 4), 'NEW', 'RELEASED', 'SHIPPED', 'INVOICED'),
       round(rand() * 4000 + 100, 2),
       elt(1 + (n % 3), 'USD', 'EUR', 'JOD'),
       now() - interval n hour
  from g;

insert into order_lines (order_id, sku, quantity, unit_price)
with recursive l (n) as (select 1 union all select n + 1 from l where n < 3)
select o.id, concat('SKU-', lpad(((o.id * 7 + l.n) % 200), 4, '0')),
       1 + ((o.id + l.n) % 9), round(rand() * 300 + 5, 2)
  from orders o cross join l;

insert into shipment_outbox (order_no, payload)
select o.order_no,
       json_object('orderNo', o.order_no, 'status', o.status,
                   'total', o.total, 'currency', o.currency)
  from orders o where o.status in ('SHIPPED', 'INVOICED');
