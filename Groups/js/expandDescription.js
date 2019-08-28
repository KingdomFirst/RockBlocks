$(function () {
  BindEventsSelfJoinTags();
});
function BindEventsSelfJoinTags() {
  $('input.sjt_profile').each(function (index) {
    $(this).click({ cb: this }, sjt_updateStates);
  });
}
function sjt_updateStates(event) {
  var box = document.getElementById(event.data.cb.id);
  var activeCount = 0;
  if (!box.checked) {
    $(box).addClass('removed');
  }
  else {
    $(box).removeClass('removed');
  }

  $('input.sjt_profile').each(function () {
    if ($(this).attr('checked'))
      activeCount += 1;
  });

  var parent = box.parentNode;
  var grandParent = box.parentNode.parentNode;
  var greatGrandParent = box.parentNode.parentNode.parentNode;

  if (box.checked) {
    $(greatGrandParent).children('div').eq(1).slideDown('slow');
  }
  else {
    $(greatGrandParent).children('div').eq(1).slideUp('slow');
  }
}
