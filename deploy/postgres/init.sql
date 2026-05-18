-- postgres/init.sql
-- 创建系统管理数据库
CREATE DATABASE mes_system;
-- 创建认证服务数据库
CREATE DATABASE mes_identity;
-- 创建公共schema
CREATE SCHEMA IF NOT EXISTS shared;
