select * from CinemaHalls
select * from users

INSERT INTO Users 
(FullName, Email, PasswordHash, Role, CreatedAt, UpdatedAt, Createdby, UpdatedBy, IsDeleted, DeletedAt)
VALUES 
('admin', 'admin@gmail.com', 'hash555', 'Admin', GETDATE(), NULL, NULL, null, 0, null)

update Users
set PasswordHash= '$2a$11$Q9kZkz7jvQ7hOeYwVnZk7uFhVYQnZk7uFhVYQnZk7uFhVYQnZk7uFhVYQnZk7u'
where id = 5

SELECT name, definition 
FROM sys.check_constraints 
WHERE parent_object_id = OBJECT_ID('dbo.Users');
