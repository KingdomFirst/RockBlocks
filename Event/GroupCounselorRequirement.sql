;with targetGroup as (
	select gm.PersonId
	from GroupMember gm
	where groupid = 1393
	--where GroupId = {{ Group.Id }}
), counselorGroups as (
	select gm.PersonId, g.Id GroupId, g.Name GroupName, gtr.IsLeader
	from groupmember gm
	inner join [group] g
	on gm.groupid = g.id
	inner join grouptype gt
	on g.GroupTypeId = gt.id
	and gt.GroupTypePurposeValueId IS NULL
	inner join grouptyperole gtr
	on gm.GroupRoleId = gtr.id
)
select a.PersonId
from counselorGroups a
where a.groupid in (
	select c.groupid
	from counselorGroups c
	inner join targetGroup t
	on c.personid = t.PersonId
	and c.IsLeader = 1
)
union all
select PersonId
from counselorGroups
where IsLeader = 1

