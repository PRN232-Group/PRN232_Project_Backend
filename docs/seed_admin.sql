/*
  Seed Admin user for InteriorStudio
  Password plain: Admin@123  (change after first login)
  Run against: InteriorStudio on (localdb)\MSSQLLocalDB
*/
USE InteriorStudio;
GO

DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Admin');
DECLARE @PasswordHash NVARCHAR(512) = N'$2a$11$0L3/bCPTOpEAmwzkoNX.Xefen0z0aJBM5bKtM0LjRFWW2WewFGpci';

IF @AdminRoleId IS NULL
BEGIN
  RAISERROR(N'Admin role missing. Run create_InteriorStudio.sql (Roles seed) first.', 16, 1);
  RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'ngthanhtrung302005@gmail.com')
BEGIN
  INSERT INTO Users (RoleId, Email, PasswordHash, FullName, Phone, IsLocked, IsActive, IsDeleted)
  VALUES (@AdminRoleId, N'ngthanhtrung302005@gmail.com', @PasswordHash, N'trung', N'0352241327', 0, 1, 0);
END
ELSE
BEGIN
  UPDATE Users
  SET PasswordHash = @PasswordHash,
      FullName = N'trung',
      Phone = N'0352241327',
      RoleId = @AdminRoleId,
      IsLocked = 0,
      IsActive = 1,
      IsDeleted = 0
  WHERE Email = N'ngthanhtrung302005@gmail.com';
END
GO
