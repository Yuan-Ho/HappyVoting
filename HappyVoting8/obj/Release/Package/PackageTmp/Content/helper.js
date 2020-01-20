var VALUE_PROP_NAME = "value";
var WHO_PROP_NAME = "who";
var TIME_PROP_NAME = "time";
var MAT_PROP_NAME = "mat";		// Short for material.
var TYPE_PROP_NAME = "type";
var PARE_PROP_NAME = "pare";		// Short for parent.
var TPT_PROP_NAME = "tpt";		// Short for tally point.

var VOTE_PAGE_PATH = "/v/";
var TAG_SCROLL_MAX_LEN = 10;

function stringToHashCode(text) {
	var hash = 0;

	for (var i = 0; i < text.length; i++) {
		var chr = text.charCodeAt(i);
		hash = ((hash << 5) - hash) + chr;
		hash |= 0;		// Convert to 32bit integer
	}
	if (hash < 0)
		hash = -hash;
	return hash;
}
function randomBySeed(seed) {
	var x = Math.sin(seed) * 10000;
	return x - Math.floor(x);
}
function stringToColor(text) {
	var hash = stringToHashCode(text);

	var r = Math.floor(randomBySeed(hash) * 200 + 28);
	var g = Math.floor(randomBySeed(hash + 1) * 200 + 28);
	var b = Math.floor(randomBySeed(hash + 2) * 200 + 28);

	return "#" + r.toString(16) + g.toString(16) + b.toString(16);
}
function errorPrint(msg) {
	console.log(msg);
}
function linkVotePage(vote_id) {
	return VOTE_PAGE_PATH + vote_id;
}
function reportXhrSuccess(callback, data, textStatus, jqXHR) {
	//console.log("data=" + data + ". textStatus=" + textStatus
	//				+ ". jqXHR.readyState=" + jqXHR.readyState + ". jqXHR.status=" + jqXHR.status
	//				+ ". jqXHR.statusText=" + jqXHR.statusText
	//				+ ". jqXHR.responseText=" + jqXHR.responseText
	//				+ ".");
	if (data.err_msg)
		callback(false, data.err_msg);
	else if (data.captcha_or_wait_seconds) {
		askCaptchaAndPostGoods(url, post_data, callback, data.captcha_or_wait_seconds);
	}
	else {
		if (typeof data == "string") {
			var rep;
			var m = data.match(/<title>(.+)<\/ ?title>/i);
			if (!m)
				rep = data.substr(0, 250);
			else
				rep = m[1];
			callback(true, rep);
		}
		else
			callback(true, data);
	}
}
function reportXhrFail(callback, jqXHR, textStatus, errorThrown) {
	console.log("errorThrown=" + errorThrown + ". textStatus=" + textStatus
                        + ". jqXHR.readyState=" + jqXHR.readyState + ". jqXHR.status=" + jqXHR.status
                        + ". jqXHR.statusText=" + jqXHR.statusText + ". jqXHR.responseText=" + jqXHR.responseText + ".");
	// For net::ERR_CONNECTION_RESET type error, it will be:
	// errorThrown=. textStatus=error. jqXHR.readyState=0. jqXHR.status=0. jqXHR.statusText=error. jqXHR.responseText=.
	var rep;
	var m = jqXHR.responseText.match(/<title>(.+)<\/ ?title>/i);
	if (!m)
		rep = jqXHR.responseText.substr(0, 250);
	else
		rep = m[1];
	callback(false, rep);
}
function postGoods(url, post_data, callback) {
	var data_to_send;
	var process_data = true;
	var content_type;

	if (post_data.upload_files) {
		data_to_send = post_data.upload_files;
		process_data = false;
		content_type = false;

		for (var name in post_data) {
			if (name === "upload_files" || !post_data.hasOwnProperty(name)) continue;
			var value = post_data[name];
			if (typeof value === "function" || typeof value === "undefined") continue;
			data_to_send.append(name, value);		// This seems changing \n to \r\n in words (on Chrome but not on IE).
		}
	}
	else
		data_to_send = post_data;

	//$.post(url, data_to_send)
	$.ajax({
		type: "POST",
		url: url,
		data: data_to_send,
		processData: process_data,
		contentType: content_type
	})
	.done(function (data, textStatus, jqXHR) {
		reportXhrSuccess(callback, data, textStatus, jqXHR);
	})
	.fail(function (jqXHR, textStatus, errorThrown) {
		reportXhrFail(callback, jqXHR, textStatus, errorThrown);
	});
}
function getGoods(url, callback) {
	$.get(url)
	.done(function (data, textStatus, jqXHR) {
		reportXhrSuccess(callback, data, textStatus, jqXHR);
		//callback(data);
	})
	.fail(function (jqXHR, textStatus, errorThrown) {
		reportXhrFail(callback, jqXHR, textStatus, errorThrown);
		//callback(null);
	});
}
function randomPick(text) {
	var idx = Math.floor(Math.random() * text.length);
	return text[idx];
}
function randomAlphaNumericCharacter() {
	return randomPick("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
}
function randomAlphaNumericString(len) {
	var text = "";
	for (var i = 0; i < len; i++)
		text += randomAlphaNumericCharacter();
	return text;
}
function fillAction(action, mat, value, type, parent_mat) {
	action[MAT_PROP_NAME] = mat;
	if (value)
		action[VALUE_PROP_NAME] = value;
	if (type)
		action[TYPE_PROP_NAME] = type;
	if (parent_mat)
		action[PARE_PROP_NAME] = parent_mat;
}
function createVote(set_public) {
	sessionPost("/api/createvote", {}, function (suc, response) {
		if (suc && response.code == ResultSuccess) {
			var vote_id = response.vote_id;
			var link = linkVotePage(vote_id);

			addVoteToScrollMy(vote_id, link);

			if (set_public === false) {
				var action = {};
				fillAction(action, "settings-public", "false", "setg");

				sessionPost("/api/addactions", {
					actions: [action],
					vote_id: vote_id,
				}, function (suc, response) {
					if (suc && response.code == ResultSuccess) {
						location.assign(link);
					}
					else {
						alert("抱歉，寫入問卷失敗。訊息：" + response + "。");
					}
				});
			}
			else {
				location.assign(link);
			}
		}
		else {
			alert("抱歉，產生新問卷失敗。訊息：" + response + "。");
		}
	});
}
function hrefToWindowName(href) {
	var pos = href.indexOf('#');
	if (pos == -1)
		return href;
	return href.substring(0, pos);
}