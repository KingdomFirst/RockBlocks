DECLARE @CounselorTypeId INT = (SELECT Id FROM GroupType WHERE [Guid] = '34F9C368-E4C4-4F29-8A97-927C957F051D')

WITH targetGroup AS (
    SELECT [gm].[PersonId]
    FROM [GroupMember] [gm]
    WHERE GroupId = {{ Group.Id }}
), counselorGroups AS (
    SELECT [gm].[PersonId],
        [g].[Id] [GroupId],
        [g].[Name] [GroupName],
        [gtr].[IsLeader]
    FROM [groupmember] [gm]
    JOIN [Group] [g]
        ON [gm].[groupid] = [g].[id]
    JOIN [grouptype] [gt]
        ON [g].[GroupTypeId] = [gt].[id]   
        AND [gt].[Guid] = @CounselorTypeId
    JOIN [grouptyperole] [gtr]
        ON [gm].[GroupRoleId] = [gtr].[id]
)
SELECT [PersonId]
FROM [counselorGroups]
WHERE [groupid] IN (
    SELECT [c].[groupid]
    FROM [counselorGroups] [c]
    JOIN [targetGroup] [t]
        ON [c].[personid] = [t].[PersonId]
        AND [c].[IsLeader] = 1
)
UNION ALL
SELECT [PersonId]
FROM [counselorGroups]
WHERE [IsLeader] = 1;

