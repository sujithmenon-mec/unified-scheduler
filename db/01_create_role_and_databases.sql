CREATE ROLE scheduler WITH LOGIN PASSWORD 'scheduler_dev_pw' CREATEDB;
CREATE DATABASE scheduler OWNER scheduler;
CREATE DATABASE scheduler_test OWNER scheduler;