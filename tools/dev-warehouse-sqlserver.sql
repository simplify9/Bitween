-- The same warehouse/order schema as dev-warehouse.sql, in T-SQL.
--
-- Deliberately the same shape, so a statement written against one engine is recognisably the same
-- job on another and the differences are the ones that are real: everything lives in a named
-- schema, a comment is an extended property, and OUTPUT does what RETURNING does.

create schema sales;
go

create table sales.customers (
    id           int identity(1,1) primary key,
    code         varchar(20)   not null unique,
    name         nvarchar(120) not null,
    country      char(2)       not null,
    credit_limit decimal(12,2) not null constraint df_credit default 0
);
go

exec sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Trading partners we ship to',
    @level0type = N'SCHEMA', @level0name = N'sales',
    @level1type = N'TABLE',  @level1name = N'customers';
go

create table sales.orders (
    id           int identity(1,1) primary key,
    order_no     varchar(30)   not null unique,
    customer_id  int           not null references sales.customers (id),
    status       varchar(20)   not null constraint df_status default 'NEW',
    total        decimal(12,2) not null,
    currency     char(3)       not null constraint df_currency default 'USD',
    placed_on    datetime2(3)  not null constraint df_placed default sysutcdatetime(),
    updated_on   datetime2(3)  not null constraint df_updated default sysutcdatetime()
);
go

exec sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Sales orders, the table Bitween writes into',
    @level0type = N'SCHEMA', @level0name = N'sales',
    @level1type = N'TABLE',  @level1name = N'orders';
go

create index ix_orders_status on sales.orders (status);
create index ix_orders_updated on sales.orders (updated_on);
go

create table sales.order_lines (
    id          int identity(1,1) primary key,
    order_id    int           not null references sales.orders (id) on delete cascade,
    sku         varchar(40)   not null,
    quantity    int           not null,
    unit_price  decimal(12,2) not null
);
go

-- The outbox an integration polls: rows appear here, Bitween drains them.
create table sales.shipment_outbox (
    id           int identity(1,1) primary key,
    order_no     varchar(30)   not null,
    payload      nvarchar(max) not null,
    created_on   datetime2(3)  not null constraint df_outbox_created default sysutcdatetime(),
    processed    bit           not null constraint df_outbox_processed default 0,
    processed_on datetime2(3)  null
);
go

exec sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Shipment notifications waiting to be picked up',
    @level0type = N'SCHEMA', @level0name = N'sales',
    @level1type = N'TABLE',  @level1name = N'shipment_outbox';
go

create index ix_outbox_pending on sales.shipment_outbox (processed, id);
go

-- A real sequence object, which MySQL does not have and PostgreSQL and Oracle do.
create sequence sales.shipment_ref_seq as bigint start with 5000 increment by 1;
go

-- An inline table-valued function: the T-SQL answer to a set-returning function, and the shape a
-- receive statement would SELECT from.
create function sales.orders_for_customer(@p_code varchar(20))
returns table
as
return (
    select o.order_no, o.status, o.total, o.placed_on
      from sales.orders o join sales.customers c on c.id = o.customer_id
     where c.code = @p_code
);
go

-- A real procedure: called, returns nothing.
create procedure sales.release_order @p_order_no varchar(30)
as
begin
    set nocount on;
    update sales.orders
       set status = 'RELEASED', updated_on = sysutcdatetime()
     where order_no = @p_order_no;
end;
go

-- And one that answers through an OUT parameter rather than a result set.
create procedure sales.count_open_orders @p_code varchar(20), @p_total int output
as
begin
    set nocount on;
    select @p_total = count(*)
      from sales.orders o join sales.customers c on c.id = o.customer_id
     where c.code = @p_code and o.status in ('NEW', 'RELEASED');
end;
go

insert into sales.customers (code, name, country, credit_limit) values
    ('ACME',    N'Acme Trading Co',    'JO', 50000),
    ('GLOBEX',  N'Globex Corporation', 'AE', 120000),
    ('INITECH', N'Initech LLC',        'US', 25000),
    ('UMBRA',   N'Umbrella Logistics', 'DE', 80000);
go

-- No generate_series before SQL Server 2022, and this has to run on 2019 too, so the rows come
-- from a recursive CTE.
with g (n) as (
    select 1 union all select n + 1 from g where n < 60
)
insert into sales.orders (order_no, customer_id, status, total, currency, placed_on)
select 'SO-' + right('00000' + cast(n as varchar(5)), 5),
       1 + (n % 4),
       choose(1 + (n % 4), 'NEW', 'RELEASED', 'SHIPPED', 'INVOICED'),
       cast(abs(checksum(newid())) % 4000 + 100 as decimal(12,2)),
       choose(1 + (n % 3), 'USD', 'EUR', 'JOD'),
       dateadd(hour, -n, sysutcdatetime())
  from g
option (maxrecursion 100);
go

with l (n) as (select 1 union all select n + 1 from l where n < 3)
insert into sales.order_lines (order_id, sku, quantity, unit_price)
select o.id,
       'SKU-' + right('0000' + cast(((o.id * 7 + l.n) % 200) as varchar(4)), 4),
       1 + ((o.id + l.n) % 9),
       cast(abs(checksum(newid())) % 300 + 5 as decimal(12,2))
  from sales.orders o cross join l;
go

insert into sales.shipment_outbox (order_no, payload)
select o.order_no,
       (select o.order_no as orderNo, o.status, o.total, o.currency
          for json path, without_array_wrapper)
  from sales.orders o
 where o.status in ('SHIPPED', 'INVOICED');
go
