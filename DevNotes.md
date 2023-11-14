# Env
mysql 8.0

# Scripts
```
# Create database and users
create user 'dba'@'%' identified by 'administrator';
create database panasonic;
grant all on panasonic to 'dba'@'%';
flush privileges;

# Create tables
use panasonic;
create table devices(
  id INT NOT NULL, 
  name VARCHAR(100) NOT NULL, 
  description VARCHAR(255), 
  PRIMARY KEY(id));

create table products(
  pcode VARCHAR(50) NOT NULL, 
  pname VARCHAR(50), 
  mcode VARCHAR(50) NOT NULL, 
  mname VARCHAR(50), 
  category INT NOT NULL, 
  codelen INT NOT NULL,
  correction TINYINT(1) NOT NULL, 
  PRIMARY KEY(pcode)));

create table history(
	id INT NOT NULL AUTO_INCREMENT,
	mcode VARCHAR(50) NOT NULL,
	tcode VARCHAR(50) NOT NULL,
	timestamp DATETIME NOT NULL,
	state BIGINT NULL,
	PRIMARY KEY(id)
);
```

