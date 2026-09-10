-- A small warehouse/order schema, the shape an ERP integration actually meets.
create table customers (
    id           serial primary key,
    code         varchar(20)  not null unique,
    name         varchar(120) not null,
    country      char(2)      not null,
    credit_limit numeric(12,2) not null default 0
);
comment on table customers is 'Trading partners we ship to';

create table orders (
    id           serial primary key,
    order_no     varchar(30)  not null unique,
    customer_id  integer      not null references customers(id),
    status       varchar(20)  not null default 'NEW',
    total        numeric(12,2) not null,
    currency     char(3)      not null default 'USD',
    placed_on    timestamptz  not null default now(),
    updated_on   timestamptz  not null default now()
);
comment on table orders is 'Sales orders, the table Bitween writes into';
create index on orders (status);
create index on orders (updated_on);

create table order_lines (
    id          serial primary key,
    order_id    integer      not null references orders(id) on delete cascade,
    sku         varchar(40)  not null,
    quantity    integer      not null,
    unit_price  numeric(12,2) not null
);

-- The outbox an integration polls: rows appear here, Bitween drains them.
create table shipment_outbox (
    id           serial primary key,
    order_no     varchar(30)  not null,
    payload      jsonb        not null,
    created_on   timestamptz  not null default now(),
    processed    boolean      not null default false,
    processed_on timestamptz
);
comment on table shipment_outbox is 'Shipment notifications waiting to be picked up';
create index on shipment_outbox (processed, id);

create sequence shipment_ref_seq start with 5000;

-- A set-returning function, the PostgreSQL answer to a REF CURSOR.
create or replace function orders_for_customer(p_code varchar)
returns table (order_no varchar, status varchar, total numeric, placed_on timestamptz)
language sql stable
as $$
    select o.order_no, o.status, o.total, o.placed_on
      from orders o join customers c on c.id = o.customer_id
     where c.code = p_code
     order by o.placed_on desc
$$;

-- A real procedure: called, returns nothing.
create or replace procedure release_order(p_order_no varchar)
language sql
as $$ update orders set status = 'RELEASED', updated_on = now() where order_no = p_order_no $$;

insert into customers (code, name, country, credit_limit) values
    ('ACME',   'Acme Trading Co',      'JO', 50000),
    ('GLOBEX', 'Globex Corporation',   'AE', 120000),
    ('INITECH','Initech LLC',          'US', 25000),
    ('UMBRA',  'Umbrella Logistics',   'DE', 80000);

insert into orders (order_no, customer_id, status, total, currency, placed_on)
select 'SO-' || lpad(g::text, 5, '0'),
       1 + (g % 4),
       (array['NEW','RELEASED','SHIPPED','INVOICED'])[1 + (g % 4)],
       round((random() * 4000 + 100)::numeric, 2),
       (array['USD','EUR','JOD'])[1 + (g % 3)],
       now() - (g || ' hours')::interval
  from generate_series(1, 60) g;

insert into order_lines (order_id, sku, quantity, unit_price)
select o.id, 'SKU-' || lpad(((o.id * 7 + l) % 200)::text, 4, '0'),
       1 + ((o.id + l) % 9), round((random() * 300 + 5)::numeric, 2)
  from orders o cross join generate_series(1, 3) l;

insert into shipment_outbox (order_no, payload)
select o.order_no,
       jsonb_build_object('orderNo', o.order_no, 'status', o.status,
                          'total', o.total, 'currency', o.currency)
  from orders o where o.status in ('SHIPPED','INVOICED');
