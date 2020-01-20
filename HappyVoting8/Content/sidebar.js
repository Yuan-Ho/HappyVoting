var TAGGED_VOTE_ICONS = ["fa-bar-chart-o", "fa-bell-o", "fa-bolt", "fa-briefcase", "fa-bullhorn", "fa-check", "fa-plus", "fa-share", "fa-shopping-cart", "fa-user"];
var TAGGED_VOTE_COLORS = ["label-info", "label-success", "label-danger", "label-default", "label-warning"];

var sg = {
	sidebarModel: null,
};

function TaggedVote(link, title, who, time) {
	this.link = link;
	this.title = title;
	this.who = who;
	this.time = time;
}
TaggedVote.prototype.time2 = function () {
	return moment(this.time).format("YYYY/M/D H:m:s");
};
TaggedVote.prototype.icon_cls = function () {
	var hash = stringToHashCode(this.link);
	return TAGGED_VOTE_ICONS[hash % TAGGED_VOTE_ICONS.length];
};
TaggedVote.prototype.icon_clr = function () {
	var hash = stringToHashCode(this.link);
	return TAGGED_VOTE_COLORS[hash % TAGGED_VOTE_COLORS.length];
};

jQuery(document).ready(function () {
	var SidebarModel = function () {
		var self = this;

		var title = $("#currLocAnchor > span:first-child").text();
		this.headingsTitle = ko.observable(title);

		this.tagScrollAll = ko.observableArray();
		this.tagScrollMy = ko.observableArray(getLocalStoredArray("tagScrollMy"));
		this.tagScrollTaken = ko.observableArray(getLocalStoredArray("tagScrollTaken"));

		this.tagScrolls = {
			"All": this.tagScrollAll,
		};

		this.sortTagScrolls = function () {
			for (var tag in self.tagScrolls) {
				var tag_scroll = self.tagScrolls[tag];

				tag_scroll.sort(function (a, b) {
					return b.time.getTime() - a.time.getTime();
				});
				tag_scroll.splice(TAG_SCROLL_MAX_LEN);
			}
		};
	};
	sg.sidebarModel = new SidebarModel();

	var $container = $(".tag_scroll_container");
	ko.applyBindings(sg.sidebarModel, $container[0]);

	//
	$("#preload_tagscrolls > ul").each(function (idx, elt) {
		var $elem = $(elt);
		var tag = $elem.attr("data-tag");

		$elem.children("li").each(function (idx2, elt2) {
			var $elem2 = $(elt2);
			processTaggedVote($elem2, tag);
		});
	});
	sg.sidebarModel.sortTagScrolls();
});
function processTaggedVote($elem, tag) {
	var $anchor = $elem.children("a");

	/*var tagged_vote = {
		link: $anchor.attr("href"),
		title: $anchor.text(),
		who: $elem.children("b").text(),
		time: new Date($elem.children("time").text()),
	};
	tagged_vote.time2 = moment(tagged_vote.time).format("YYYY/M/D H:m:s");

	var hash = stringToHashCode(tagged_vote.link);

	tagged_vote.icon_cls = TAGGED_VOTE_ICONS[hash % TAGGED_VOTE_ICONS.length];
	tagged_vote.icon_clr = TAGGED_VOTE_COLORS[hash % TAGGED_VOTE_COLORS.length];*/

	var tagged_vote = new TaggedVote($anchor.attr("href"),
										$anchor.text(),
										$elem.children("b").text(),
										new Date($elem.children("time").text()));

	var tag_scroll = sg.sidebarModel.tagScrolls[tag];
	if (tag_scroll)
		tag_scroll.push(tagged_vote);
}
function updateLocalTagScroll(scroll, vote_id, title) {
	var link = linkVotePage(vote_id);

	var arr = getLocalStoredArray(scroll);
	var modified = false;

	arr.forEach(function (x, i, a) {
		if (x.link == link) {
			if (x.title != title) {
				x.title = title;
				modified = true;
			}
		}
	});
	if (modified)
		setLocalStoredArray(scroll, arr);
	return modified;
}
function addVoteToScrollTaken() {
	/*var tagged_vote = {
		link: linkVotePage(getVoteId()),
		title: gl.pageModel.headingsTitle(),
		who: gl.pageModel.ownerWho(),
		time: new Date(),
	};*/
	var tagged_vote = new TaggedVote(linkVotePage(getVoteId()),
										gl.pageModel.headingsTitle(),
										gl.pageModel.ownerWho(),
										new Date());

	addVoteToScroll(tagged_vote, "tagScrollTaken");
}
function submitTags() {
	//var title = $('#headings-title').editable("getValue", true);
	//var is_public = $('#chkSettingPublic').bootstrapSwitch('state');
	var title = getActionValue("headings-title");
	var set_public = getActionValue("settings-public") == "true";

	if (!title || !set_public)
		return;

	sessionPost("/api/addvotetotags", {
		tags: ["All"],
		vote_id: getVoteId(),
		title: title,
	}, function (suc, response) {
		if (suc && response.code == ResultSuccess) {
		}
		else {
			topAlert("抱歉，加入Tag失敗。訊息：" + response + "。");
		}
	});
}
function getLocalStoredArray(name) {
	var value = localStorage[name];
	if (!value)
		return [];
	var arr = JSON.parseWithDate(value);
	return arr;
}
function setLocalStoredArray(name, arr) {
	localStorage[name] = JSON.stringify(arr);
}
function addVoteToScroll(tagged_vote, scroll) {
	var arr = getLocalStoredArray(scroll);

	if (!arr.some(function (x) {
		return x.link == tagged_vote.link;
	})) {
		arr.unshift(tagged_vote);
		if (arr.length > TAG_SCROLL_MAX_LEN)
			arr.pop();

		setLocalStoredArray(scroll, arr);
	}
}
function addVoteToScrollMy(vote_id, link) {
	/*var tagged_vote = {
		link: link,
		title: "新投票",
		who: getUserName(),
		time: new Date(),
	};*/
	var tagged_vote = new TaggedVote(link,
										"新投票",
										getUserName(),
										new Date());

	addVoteToScroll(tagged_vote, "tagScrollMy");
}
function onNewTaggedVote(data) {
	for (var i = 0; i < data.tags.length; i++) {
		var tag = data.tags[i];

		var tagged_vote = new TaggedVote(linkVotePage(data.vote_id),
										data.title,
										data.who,
										new Date(data.time));

		var tag_scroll = sg.sidebarModel.tagScrolls[tag];
		if (tag_scroll)
			tag_scroll.unshift(tagged_vote);
	}
}