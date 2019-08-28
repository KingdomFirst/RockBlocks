CREATE PROCEDURE [dbo].[_rocks_kfs_spUtility_SetAttributeValue]
(@AttributeGuid          UNIQUEIDENTIFIER, 
 @EntityId               INT, 
 @AttributeValue         NVARCHAR(MAX), 
 @CreatedByPersonAliasId INT              = NULL
)
AS
     BEGIN
         DECLARE @AttributeId INT=
         (
             SELECT [Id]
             FROM [Attribute]
             WHERE [Guid] = @AttributeGuid
         );
         INSERT INTO [dbo].[AttributeValue]
         ([IsSystem], 
          [AttributeId], 
          [EntityId], 
          [Value], 
          [Guid], 
          [CreatedDateTime], 
          [CreatedByPersonAliasId]
         )
         VALUES
         (0, 
          @AttributeId, 
          @EntityId, 
          @AttributeValue, 
          NEWID(), 
          GETDATE(), 
          @CreatedByPersonAliasId
         );
     END;
GO