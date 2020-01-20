var TALLY_POINT_PRECISION = 100;
var PAST_TIME = new Date(1996, 0, 5).toString();
var NOT_YET_TIME = new Date(1996 + 50, 0, 5).toString();
var STATE_BUTTON_TEXTS = ["發佈", "結束投票", "關閉頁面"];
var STATE_TIME_SETTINGS = ["settings-publish-time", "settings-finish-time", "settings-close-time"];
var NEXT_STATE_CONFIRM_TEXTS = ["發佈後即開始進行投票，是否確定發佈？", "按下確定以結束投票。", "按下確定以關閉頁面（只有投票發起人可以看到投票結果，其它人都無法再查看投票結果。）"];

//var pending_actions = [];
var got_actions = {};

var gl = {
	processing: false,
	pageModel: null,
};
function findAction(actions, mat) {
	var found_idx = -1;

	actions().some(function (x, i) {
		if (x[MAT_PROP_NAME] == mat) {
			found_idx = i;
			return true;
		}
		else
			return false;
	});
	return found_idx;
}
function updateAction(actions, mat, value, type, parent_mat) {
	if (gl.processing)
		return;
	if (type == "setg") {
		if (!gl.pageModel.isOwner()) {
			topAlert("抱歉，只有擁有者可以修改。");
			return;
		}
	}
	else {
		var old_action = got_actions[mat];
		if (old_action) {
			if (getUserName() != old_action[WHO_PROP_NAME]) {
				topAlert("抱歉，只有原作者可以修改。");
				return;
			}
		}
	}
	var found_idx = findAction(actions, mat);
	if (found_idx == -1) {
		found_idx = actions.push({}) - 1;
	}
	var action = actions()[found_idx];

	fillAction(action, mat, value, type, parent_mat);

	//$("#NumPendingActions").text("(" + pending_actions.length + ")");
}
function processAction($elem) {
	// <p data-mat='UbA6Gn' data-who='_Oz7cfqsO' data-time='2016-03-02T16:19:24.1286483+00:00' data-type='ansr' data-pare='6yh1aJ'>第二個選項</p>
	var mat = $elem.attr("data-mat");

	var action = {
		who: $elem.attr("data-who"),
		time: new Date($elem.attr("data-time")),
		value: $elem.text(),
		type: $elem.attr("data-type"),
		pare: $elem.attr("data-pare"),
	};
	if (action.type == "setg") {
		if (action[WHO_PROP_NAME] != gl.pageModel.ownerWho()) {
			console.log("Who of " + mat + " mismatch! " + action[WHO_PROP_NAME] + " (new) != " + gl.pageModel.ownerWho() + " (owner). Ignore.");
			return;
		}
	}
	else {
		var old_action = got_actions[mat];
		if (old_action) {
			if (action[WHO_PROP_NAME] != old_action[WHO_PROP_NAME]) {
				console.log("Who of " + mat + " mismatch! " + action[WHO_PROP_NAME] + " (new) != " + old_action[WHO_PROP_NAME] + " (old). Ignore.");
				return;
			}
		}
	}
	got_actions[mat] = action;
	realizeAction(mat);
}
function realizeAction(mat) {
	var action = got_actions[mat];

	if (action) {
		var is_who = action[WHO_PROP_NAME] == getUserName();

		if (action.type == "qstn") {
			var $sbjt = updateSubject(mat, action.value);
			if (!$sbjt)
				$sbjt = addSubject(mat, action.value, is_who);
		}
		else if (action.type == "ansr") {
			var $ansr = updateAnswer(mat, action.value);
			if (!$ansr)
				$ansr = addAnswer(action.pare, mat, action.value, is_who);
		}
		else if (action.type == "cmmt") {
			var $target = $("#" + action.pare);
			if ($target.length) {
				var model = $target.data("ViewModel");
				model.addComment(action[WHO_PROP_NAME], action.value);
			}
		}
		else if (action.type == "pict") {
			insertPictAttachment(action.pare, action.value);
		}
		else if (mat == "headings-title") {
			$('#headings-title').editable("setValue", action.value)/*.editable(is_who ? "enable" : "disable")*/;
			gl.pageModel.headingsTitle(action.value);
		}
		else if (mat == "headings-desc") {
			$('#headings-desc').editable("setValue", action.value)/*.editable(is_who ? "enable" : "disable")*/;
		}
		else if (mat == "settings-public") {
			$elem = $('#chkSettingPublic');
			var orig_disabled = $elem.bootstrapSwitch('disabled');

			$elem.bootstrapSwitch('disabled', false)		// otherwise the state cannot be set.
				.bootstrapSwitch('state', action.value == "true", true)
				.bootstrapSwitch('disabled', orig_disabled/*!is_who*/);
		}
		else if (mat == "infos-create")
			gl.pageModel.ownerWho(action[WHO_PROP_NAME]);
		else if (STATE_TIME_SETTINGS.indexOf(mat) != -1)
			/*else if (mat == "settings-publish-time" ||
				mat == "settings-finish-time" ||
				mat == "settings-close-time")*/
			gl.pageModel.refreshTime();
		return true;
	}
	return false;
}
function getActionValue(mat) {
	var found_idx = findAction(gl.pageModel.pendingActions, mat);

	if (found_idx != -1) {
		var action = gl.pageModel.pendingActions()[found_idx];
		return action.value;
	}

	var action = got_actions[mat];
	if (action)
		return action.value;

	// Default values
	if (STATE_TIME_SETTINGS.indexOf(mat) != -1)
		return NOT_YET_TIME;

	switch (mat) {
		case "settings-public": return "true";
			/*case "settings-publish-time": return NOT_YET_TIME;
			case "settings-finish-time": return NOT_YET_TIME;
			case "settings-close-time": return NOT_YET_TIME;*/
	}
}
function getVoteId() {
	// ie. /v/7j6ibCCC
	var comps = location.pathname.split("/");
	return comps[2];
}
function pulsateOnShow($elem) {
	if (jQuery().pulsate) {
		$elem.find('.pulsate_on_show').pulsate({
			color: "#fdbe41",
			reach: 15,
			repeat: 3,
			speed: 1000,
			glow: true
		});
	}
};
function handlePulsate() {
	if (!jQuery().pulsate) {
		return;
	}
	if (App.isIE8() == true) {
		return; // pulsate plugin does not support IE8 and below
	}
	pulsateOnShow($('#insert_sbjt_here'));
};
function initEditables() {
	$.fn.editable.defaults.mode = 'inline';
	$.fn.editable.defaults.inputclass = 'form-control';
	$.fn.editable.defaults.onblur = 'submit';

	$('#headings-title').editable({
		type: 'text',
		emptytext: "請點此輸入投票標題",
		placeholder: "請點此輸入投票標題",
		inputclass: 'form-control input-large input-lg',
		highlight: "#FFFFFF",
		tpl: "<input type='text' maxlength='100'>",
		validate: function (value) {
			value = $.trim(value);
			if (!value)
				return '此欄位為必填。';
		},
	}).on('save', function (e, params) {
		updateAction(gl.pageModel.pendingActions, "headings-title", params.newValue, "setg");
	});
	$('#headings-desc').editable({
		type: "textarea",
		emptytext: "請點此輸入投票說明",
		placeholder: "請點此輸入投票說明",
		inputclass: 'form-control input-large',
		highlight: "#FFFFFF",
		tpl: "<textarea maxlength='1000'></textarea>",
		validate: function (value) {
			value = $.trim(value);
			if (!value)
				return '此欄位為必填。';
		},
		display: function (value) {
			value = $.fn.editableutils.escape(value);
			var html = value.autoLink({ target: "_blank" });
			$(this).html(html);
		},
	}).on('save', function (e, params) {
		updateAction(gl.pageModel.pendingActions, "headings-desc", params.newValue, "setg");
	});
};
function addAnswer(sbjt_mat, ansr_mat, ansr_text, is_who) {
	var $sbjt = $("#" + sbjt_mat);

	var sbjt_model = $sbjt.data("ViewModel");
	var subject_ordinal = sbjt_model.subjectOrdinal;

	var $anss = $sbjt.find(".icheck-list");
	var $ansr = $("#ansr_t").clone(true);		// true in clone for iCheck('destroy') to work.

	$ansr.attr("id", ansr_mat);		// override original id.

	var $radio = $ansr.find(":radio");
	$radio.attr("name", "anss_" + subject_ordinal);
	$radio.iCheck('destroy');

	$anss.append($ansr);

	$radio.iCheck({
		radioClass: $radio.attr('data-radio'),
	});
	$radio.iCheck(sbjt_model.canVote.peek() ? 'enable' : 'disable');

	var $ansr_ta = $ansr.find('textarea');
	$ansr_ta.maxlength({ alwaysShow: true });
	setTimeout(autosize, 0, $ansr_ta);		// call autosize after the content of textarea has been written so that initial height is expanded.

	var answer_ordinal = $anss.children().length;		// start from 1
	var tally_obj = createTally(sbjt_mat, ansr_mat, "選項 " + answer_ordinal);
	tally_obj.modifyText(ansr_text);

	$ansr.data("TallyObject", tally_obj);

	function AnswerViewModel() {
		var self = this;

		this.qaId = subject_ordinal + "." + answer_ordinal;
		this.ph = "請點此輸入選項 " + this.qaId + "。";
		this.answerText = ko.observable(ansr_text);
		this.editing = sbjt_model.editing;
		this.isWho = is_who;

		this.modifyTextTimerId = null;
		this.commentArray = ko.observableArray();

		this.answerText.subscribe(function (newValue) {
			if (self.modifyTextTimerId) {
				clearTimeout(self.modifyTextTimerId);
				self.modifyTextTimerId = null;
			}
			self.modifyTextTimerId = setTimeout(function () {
				tally_obj.modifyText(newValue);		// slow and making user feel delay.
				self.modifyTextTimerId = null;
			}, 2000);

			updateAction(sbjt_model.actions, ansr_mat, newValue, "ansr", sbjt_mat);
		});
		this.deleteMe = function () {
			if (self.modifyTextTimerId) {
				clearTimeout(self.modifyTextTimerId);
				self.modifyTextTimerId = null;
			}
			var $dropdown = $ansr.find("#ansr_tool_menu");
			if ($dropdown.length)
				$dropdown.prependTo("#hidden_container");

			deleteTally(tally_obj);
			$ansr.remove();
		};
		this.updateAttachment = function (value, type) {
			updateAction(sbjt_model.actions, ansr_mat + "-atta", value, type, ansr_mat);
		};
		this.canAddPicture = ko.pureComputed(function () {
			var answer_len = self.answerText().length;
			var editing = self.editing();		// If editing() is not run during evaluation, the dependency chain will miss it.

			return editing && self.isWho && answer_len > 0;
		});
		this.addComment = function (who, text) {
			var co = new CommentObject(who, text);
			self.commentArray.push(co);
		};
	};
	var model = new AnswerViewModel();
	ko.applyBindings(model, $ansr[0]);

	$ansr.data("ViewModel", model);

	$radio.on('ifChecked', function (event) {
		if (gl.processing) {
			sbjt_model.onVoted(ansr_mat);
		}
		else
			sbjt_model.pendingTally(ansr_mat);
	});
	return $ansr;
};
function processTotal(total) {
	var $ansr = $("#" + total.ansr_mat);
	if ($ansr.length) {
		var tally_obj = $ansr.data("TallyObject");
		tally_obj.setTally(total.tpt);
	}
}
function processTally(tally) {
	if (tally.tpt > 0) {
		var $ansr = $("#" + tally.ansr_mat);
		if ($ansr.length) {
			var $radio = $ansr.find(":radio");
			$radio.iCheck('check');
		}
	}
}
function updateAnswer(ansr_mat, ansr_text) {
	var $ansr = $("#" + ansr_mat);
	if (!$ansr.length)
		return null;

	var model = $ansr.data("ViewModel");
	model.answerText(ansr_text);

	return $ansr;
}
function addSubject(sbjt_mat, qstn_text, is_who) {
	var subject_ordinal = $(".sbjt_c").length;		// start from 1
	var $sbjt = $("#sbjt_t").clone();

	$sbjt.removeClass("hidden");
	$sbjt.attr("id", sbjt_mat);		// override original id.

	var $anss = $sbjt.find(".icheck-list");
	$anss.empty();

	var $tgt = $("#insert_sbjt_here");
	$sbjt.insertBefore($tgt);

	initCharts($sbjt);

	var $qstn_ta = $sbjt.find('textarea');
	$qstn_ta.maxlength({ alwaysShow: true });
	setTimeout(autosize, 0, $qstn_ta);		// call autosize after the content of textarea has been written so that initial height is expanded.

	function SubjectViewModel() {
		var self = this;

		this.subjectOrdinal = subject_ordinal;
		this.questionText = ko.observable(qstn_text);
		this.pendingTally = ko.observable("");
		this.votedTally = ko.observable("").extend({ notify: 'always' });
		this.actions = ko.observableArray();
		this.editing = ko.observable(qstn_text == "").extend({ notify: 'always' });
		this.isWho = is_who;
		this.voteState = gl.pageModel.voteState;

		this.canVote = ko.pureComputed(function () {
			var vote_state = self.voteState();
			var editing = self.editing();		// If editing() is not run during evaluation, the dependency chain will miss it.

			return !self.votedTally() && !editing && vote_state == 1/*投票進行中*/;
		});
		this.canVote.extend({ notify: 'always' })
			.subscribe(function (newValue) {
				var $radio = $anss.find(":radio");
				$radio.iCheck(newValue ? 'enable' : 'disable');
			});
		this.onAddAnswer = function () {
			if (self.questionText().length == 0)
				$qstn_ta.focus();
			else {
				var ansr_mat = randomAlphaNumericString(6);
				var $ansr = addAnswer(sbjt_mat, ansr_mat, "", true);

				self.editing(true);		// put after adding answer so that check input is disabled.

				$ansr.find("textarea").focus();
			}
		};
		this.questionText.subscribe(function (newValue) {
			updateAction(self.actions, sbjt_mat, newValue, "qstn");
		});
		this.actions.subscribe(function (newValue) {
			if (newValue.length == 0)
				self.editing(false);		// actions submit.
			else
				self.editing(true);			// submit failed. actions restored.
		});
		this.alert = function (message) {
			App.alert({
				container: $sbjt.find("form"),
				place: 'append',
				type: 'warning',
				message: message,
				close: true,
				reset: false,
				focus: true,
				//closeInSeconds: 10,
				icon: 'warning'
			});
		};
		this.onResetTally = function () {
			bootbox.confirm({
				title: '重設投票',
				message: "此題目您已經投過票了。重設投票會清除投票結果，讓您可以重新投票。是否要重設投票？",
				callback: function (result) {
					if (result)
						sessionPost("/api/resettallies",
							{
								sbjt_mat: sbjt_mat,
								vote_id: getVoteId(),
							}, function (suc, response) {
								if (suc && response.code == ResultSuccess) {
									self.votedTally("");
									var $radio = $anss.find(":radio");
									//$radio.iCheck('enable');
									$radio.iCheck('uncheck');
								}
								else {
									self.alert("抱歉，重設投票失敗。訊息：" + response + "。");
								}
							});
				}
			});
		};
		this.onVoted = function (ansr_mat) {
			self.votedTally(ansr_mat);
			//var $radio = $anss.find(":radio");
			//$radio.iCheck('disable');
		};
		this.onCancelTally = function () {
			var $radio = $anss.find(":radio");
			$radio.iCheck('uncheck');
			this.pendingTally("");
		};
		this.onSubmitTally = function () {
			var ansr_mat = self.pendingTally();
			self.pendingTally("");		// Make the button disable.

			if (ansr_mat) {
				var tally = {};
				tally[MAT_PROP_NAME] = ansr_mat;
				tally[TPT_PROP_NAME] = TALLY_POINT_PRECISION;

				sessionPost("/api/addtallies", {
					sbjt_mat: sbjt_mat,
					tallies: [tally],
					vote_id: getVoteId(),
				}, function (suc, response) {
					if (suc && response.code == ResultSuccess) {
						//self.pendingTally("");
						self.votedTally(ansr_mat);
					}
					else {
						self.pendingTally(ansr_mat);
						self.alert("抱歉，投票失敗。訊息：" + response + "。");
					}
				});
				addVoteToScrollTaken();
			}
		};
		this.onCancelEdit = function () {
			gl.processing = true;

			$anss.children().each(function (idx, elt) {
				var $ansr = $(elt);
				var ansr_mat = $ansr.attr("id");

				if (!realizeAction(ansr_mat)) {
					var model = $ansr.data("ViewModel");
					model.deleteMe();
				}
			});
			if (!realizeAction(sbjt_mat)) {
				var $dropdown = $sbjt.find("#qstn_tool_menu");
				if ($dropdown.length)
					$dropdown.prependTo("#hidden_container");

				deleteCharts(sbjt_mat);
				$sbjt.remove();
			}
			gl.processing = false;
			self.actions.removeAll();
		};
		this.onStartEdit = function () {
			self.editing(true);
			$qstn_ta.focus();
		};
		this.updateAttachment = function (value, type) {
			updateAction(self.actions, sbjt_mat + "-atta", value, type, sbjt_mat);
		};
	};
	var model = new SubjectViewModel();
	ko.applyBindings(model, $sbjt[0]);

	$sbjt.data("ViewModel", model);

	return $sbjt;
};
function updateSubject(sbjt_mat, qstn_text) {
	var $sbjt = $("#" + sbjt_mat);
	if (!$sbjt.length)
		return null;

	var model = $sbjt.data("ViewModel");
	model.questionText(qstn_text);

	return $sbjt;
}
function pageInit() {
	handlePulsate();
	initEditables();

	$('#chkSettingPublic').on('switchChange.bootstrapSwitch', function (event, state) {
		updateAction(gl.pageModel.pendingActions, "settings-public", state ? "true" : "false", "setg");
	});
	var this_page_link = linkVotePage(getVoteId());
	$("#currLocAnchor").attr("href", this_page_link);

	// Override demo.js.
	var panel = $('.theme-panel');
	$('.theme-colors > ul > li', panel).click(function () {
		var color = $(this).attr("data-style");
		if (color == 'light2') {
			$('.page-logo img').attr('src', '../Content/logo-1.png');
		} else {
			$('.page-logo img').attr('src', '../Content/logo-1.png');
		}
	});
	//
	ko.bindingHandlers.stopBinding = {
		init: function () {
			return { controlsDescendantBindings: true };
		}
	};
	ko.virtualElements.allowedBindings.stopBinding = true;

	var PageModel = function () {
		var self = this;

		this.pendingActions = ko.observableArray();
		this.currentTime = ko.observable(new Date().getTime()).extend({ notify: 'always' });

		this.voteState = ko.pureComputed(function () {
			var now = self.currentTime();

			for (var i = 0; i < STATE_TIME_SETTINGS.length; i++) {
				var mat = STATE_TIME_SETTINGS[i];
				var value = getActionValue(mat);

				if (now < new Date(value).getTime())
					break;
			}
			return i;
		});
		this.tagScrollAll = sg.sidebarModel.tagScrollAll;

		this.headingsTitle = sg.sidebarModel.headingsTitle;
		this.headingsTitle.subscribe(function (newValue) {
			document.title = newValue + " | www.fotous.net";

			if (updateLocalTagScroll("tagScrollMy", getVoteId(), newValue))
				sg.sidebarModel.tagScrollMy(getLocalStoredArray("tagScrollMy"));
		});

		this.ownerWho = ko.observable("");
		this.isOwner = ko.pureComputed(function () {
			return getUserName() == self.ownerWho();
		}, this).extend({ notify: 'always' });

		this.isOwner.subscribe(function (newValue) {
			$('#headings-title').editable(newValue ? "enable" : "disable");
			$('#headings-desc').editable(newValue ? "enable" : "disable");
			$('#chkSettingPublic').bootstrapSwitch('disabled', !newValue);
		});

		this.onNextState = function () {
			if (!checkHeadingInput())
				return;
			var vote_state = self.voteState();

			var set_public = getActionValue("settings-public") == "true";
			var warning = vote_state == 0 ? "<p>您選擇 <span class='label " + (set_public ? "label-danger" : "label-success") +
											"'>" + (set_public ? "" : "不") + "公開</span> 投票。</p>" : "";

			bootbox.confirm({
				title: '下一步',
				message: warning + NEXT_STATE_CONFIRM_TEXTS[vote_state],
				callback: function (result) {
					if (result) {
						// Put before submitting actions so that title can be got.
						if (vote_state == 0)		// 發佈
							submitTags();

						var mat = STATE_TIME_SETTINGS[vote_state];

						updateAction(gl.pageModel.pendingActions, mat, new Date().toString(), "setg");
						submitActions(gl.pageModel.pendingActions);
					}
				}
			});
		};
		this.refreshTime = function () {
			self.currentTime(new Date().getTime());
		};
		setInterval(this.refreshTime, 5000);
	};
	gl.pageModel = new PageModel();
	ko.applyBindings(gl.pageModel);
};
function checkHeadingInput() {
	var $header = $("#headings-title");
	//if ($header.hasClass("editable-empty")) {
	if (!getActionValue("headings-title")) {
		App.scrollTo($header, -200);
		$header.editable("show");
		return false;		// return false so that the input get focused on IE.
	}
	$header = $("#headings-desc");
	//if ($header.hasClass("editable-empty")) {
	if (!getActionValue("headings-desc")) {
		App.scrollTo($header, -200);
		$header.editable("show");
		return false;		// return false so that the textarea get focused on IE.
	}
	return true;
}
function onAddSubject() {
	if (!checkHeadingInput())
		return false;

	var sbjt_mat = randomAlphaNumericString(6);

	var $sbjt = addSubject(sbjt_mat, "", true);

	setTimeout(function () {		// for IE. Otherwise focus won't work.
		$sbjt.find("textarea").focus();
		pulsateOnShow($sbjt);
	}, 0);
};
function onCancelSetting() {
	gl.processing = true;

	gl.pageModel.pendingActions().forEach(function (x, i) {
		var mat = x[MAT_PROP_NAME];
		realizeAction(mat);
	});
	gl.processing = false;
	gl.pageModel.pendingActions.removeAll();
}
function submitHeadingActions() {
	if (checkHeadingInput())
		submitActions(gl.pageModel.pendingActions);
}
function submitActions(actions) {
	if (actions().length == 0)
		return;

	var aa = actions.removeAll();		// Make button disable.

	sessionPost("/api/addactions", {
		actions: aa,		//actions(),
		vote_id: getVoteId(),
	}, function (suc, response) {
		if (suc && response.code == ResultSuccess) {
			//actions.removeAll();
		}
		else {
			for (var i = 0; i < aa.length; i++)
				actions.push(aa[i]);
			topAlert("抱歉，寫入問卷失敗。訊息：" + response + "。");
		}
	});
};
function topAlert(message) {
	App.alert({
		place: 'append',
		type: 'danger',
		message: message,
		close: true,
		reset: false,
		focus: true,
		//closeInSeconds: 10,
		icon: 'warning'
	});
}
function downloadMyTallies() {
	sessionGet("/gapi/gettallies", {
		vote_id: getVoteId(),
	}, function (suc, response) {
		if (suc && response.code == ResultSuccess) {
			gl.processing = true;
			for (var i = 0; i < response.tallies.length; i++)
				processTally(response.tallies[i]);
			gl.processing = false;
		}
		else {
			topAlert("抱歉，下載您的投票結果失敗。訊息：" + response + "。");
		}
	});
}
function downloadTotals() {
	simpleGet("/gapi/gettotals", {
		vote_id: getVoteId(),
	}, function (suc, response) {
		if (suc && response.code == ResultSuccess) {
			for (var i = 0; i < response.totals.length; i++)
				processTotal(response.totals[i]);
		}
		else {
			topAlert("抱歉，下載投票結果失敗。訊息：" + response + "。");
		}
	});
}
function onUpdateTotals(totals) {
	for (var i = 0; i < totals.length; i++)
		processTotal(totals[i]);
}
function initializeConnection() {
	var vote_hub = $.connection.voteHub;
	vote_hub.client.onNewActions = onNewActions;
	vote_hub.client.onUpdateTotals = onUpdateTotals;
	vote_hub.client.onNewTaggedVote = onNewTaggedVote;

	var currentState = $.signalR.connectionState.disconnected;		// connecting, connected, reconnecting, or disconnected.

	$('#ctp_auto_refresh').on('ifToggled', function (event) {
		console.log("ifToggled. this.checked=" + this.checked + ", currentState=" + currentState);

		if (this.checked && currentState == $.signalR.connectionState.disconnected)
			$.connection.hub.start();
		else if (!this.checked && currentState == $.signalR.connectionState.connected)
			$.connection.hub.stop();
	});
	var tryingToReconnect = false;

	$.connection.hub.reconnecting(function () {
		console.log("reconnecting. tryingToReconnect=" + tryingToReconnect);
		tryingToReconnect = true;
	});
	$.connection.hub.reconnected(function () {
		console.log("reconnected. tryingToReconnect=" + tryingToReconnect);
		tryingToReconnect = false;
	});
	$.connection.hub.disconnected(function () {
		console.log("disconnected. tryingToReconnect=" + tryingToReconnect);
		if (tryingToReconnect) {
			$('#ctp_auto_refresh').iCheck('uncheck');
		}
	});
	$.connection.hub.stateChanged(function (change) {
		console.log("stateChanged. currentState=" + currentState + ", change.newState=" + change.newState +
			", connection.id=" + $.connection.hub.id + ", connection.messageId=" + $.connection.hub.messageId);
		currentState = change.newState;

		if (change.newState === $.signalR.connectionState.disconnected) {
			$('#ctp_auto_refresh').iCheck('uncheck');
		}
		else if (change.newState === $.signalR.connectionState.connected) {
			$('#ctp_auto_refresh').iCheck('check');
		}
	});
	// $.connection.hub.logging = true;
	//$.connection.hub.start().done(onConnectionDone);
	$.connection.hub.start(onConnectionDone);
}
jQuery(document).ready(function () {
	pageInit();
	populate();

	downloadMyTallies();
	//downloadTotals();

	initializeConnection();

	setTimeout(unitTest, 1000);
});
function onConnectionDone() {
	// will be called everytime connection is started/restarted.
	console.log("onConnectionDone");
	enterVoteRoom();

	setTimeout(function () {
		$('#ctp_auto_refresh').iCheck('uncheck');
	}, 30 * 60 * 1000);
}
function enterVoteRoom() {
	// will be called everytime connection is started/restarted.
	var vote_id = getVoteId();

	try {
		var vote_hub = $.connection.voteHub;
		vote_hub.server.enterVoteRoom(vote_id).done(function () {
		}).fail(function (error) {
			errorPrint("vote_hub.server.enterVoteRoom fail - " + error);
		});
	}
	catch (ex) {
		errorPrint("vote_hub.server.enterVoteRoom threw an error - " + ex.name + ": " + ex.message + ".");
	}
}
function onNewActions(hps) {
	gl.processing = true;

	for (var i = 0; i < hps.length; i++) {
		var hp = hps[i];
		var $elem = $(hp);
		processAction($elem);
	}
	gl.processing = false;
}
function populate() {
	gl.processing = true;

	$("#preload_paper > p").each(function (idx, elt) {
		var $elem = $(elt);
		processAction($elem);
	});
	if (preloadTotals)
		onUpdateTotals(preloadTotals);

	gl.processing = false;
}
