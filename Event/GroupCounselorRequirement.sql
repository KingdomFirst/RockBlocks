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
    INNER JOIN [Group] [g]
    ON [gm].[groupid] = [g].[id]
    INNER JOIN [grouptype] [gt]
    ON [g].[GroupTypeId] = [gt].[id]
    AND [gt].[GroupTypePurposeValueId] IS NULL
    INNER JOIN [grouptyperole] [gtr]
    ON [gm].[GroupRoleId] = [gtr].[id]
)
SELECT [a].[PersonId]
FROM [counselorGroups] [a]
WHERE [a].[groupid] IN (
    SELECT [c].[groupid]
    FROM [counselorGroups] [c]
    INNER JOIN [targetGroup] [t]
    ON [c].[personid] = [t].[PersonId]
    AND [c].[IsLeader] = 1
)
UNION ALL
SELECT [PersonId]
FROM [counselorGroups]
WHERE [IsLeader] = 1;

