CREATE DATABASE RebusOutboxSample;

go 

create table Orders 
(
    Id bigint identity primary key,
    ProductId nvarchar(100),
    Quantity int
)

go