DECLARE @FamilyGroupTypeId INT = (SELECT Id FROM GroupType WHERE [Guid] = '790E3215-3B10-442B-AF69-616C0DCB998E')
DECLARE @HostHomeTypeId INT = (SELECT Id FROM GroupType WHERE [Guid] = 'F58D293E-AA58-4A1B-86FA-AC15AE0D6F62')
DECLARE @AdultRoleId INT = (SELECT Id FROM GroupTypeRole WHERE [Guid] = '2639F9A5-2AAE-4E48-A8C3-4FFE86681E42')
DECLARE @HostRoleId INT = (SELECT Id FROM GroupTypeRole WHERE [Guid] = '8EAC446B-5FE8-4998-8FC8-F46462DFED90')
DECLARE @BackgroundCheckResultId INT = (SELECT Id FROM Attribute WHERE [Guid] = '44490089-E02C-4E54-A456-454845ABBC9D')
DECLARE @PassValues VARCHAR(50) = 'Pass'

;WITH hostInfo AS (
    SELECT [hgm].[PersonId],
        [fg].[Id] [FamilyGroupId],
        [fg].[Name] [FamilyGroupName]
    FROM [GroupMember] [hgm]
    JOIN [GroupMember] [fgm]
        ON [hgm].GroupId = {{ Group.Id }}
        AND [fgm].PersonId = [hgm].PersonId
    JOIN [Group] [fg]
        ON [fg].Id = [fgm].GroupId
        AND [fg].GroupTypeId = @FamilyGroupTypeId
    WHERE [hgm].GroupRoleId = @HostRoleId
), adultInfo AS (
    SELECT [gm].[PersonId], h.FamilyGroupId, ISNULL(av.Value, '') Result
    FROM [GroupMember] [gm]
    JOIN [hostInfo] h
        ON [gm].GroupId = h.[FamilyGroupId]
    LEFT JOIN [AttributeValue] av
        ON [av].AttributeId = @BackgroundCheckResultId
        AND gm.PersonId = av.EntityId
    WHERE [gm].GroupRoleId = @AdultRoleId
), intersection as (
    SELECT FamilyGroupId
    FROM adultInfo p
    WHERE Result = @PassValues
    EXCEPT 
    SELECT FamilyGroupId
    FROM adultInfo f
    WHERE Result <> @PassValues
)
SELECT DISTINCT a.PersonId
FROM adultInfo a
JOIN intersection i
    ON a.FamilyGroupId = i.FamilyGroupId



