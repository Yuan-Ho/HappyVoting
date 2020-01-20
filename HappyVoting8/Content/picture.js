var RECENT_PICT_COUNT = 15;
var HEAD_ICONS = ["anaconda", "antelope", "apatosaurus", "baboon", "bear", "bison", "black_panther", "blue_whale", "boar", "boxer_dog", "buffalo", "bull", "bulldog", "butterflyfish", "camel", "cardinal", "cat", "chicken", "chimera", "chimpanzee", "chinese_dragon", "chipmunk", "cockatoo", "cougar", "cow", "crow", "deer", "doberman", "donkey", "duck", "eagle", "elephant", "falcon", "finch", "fish", "flamingo", "fox", "frog", "gazelle", "german_shepherd", "giraffe", "goat", "golden_retriever", "goldfish", "goose", "gorilla", "greyhound", "griffin", "guinea_pig", "gull", "hare", "hen", "heron", "herring", "hippopotamus", "horse", "howling_monkey", "hyena", "ibex", "iguana", "jackal", "kangaroo", "killer_whale", "kingfisher", "koala", "lion", "llama", "lovebird", "macaw", "mammoth", "meerkat", "moose", "ostrich", "otter", "owl", "panda", "parakeet", "parrot", "pelican", "penguin", "pheasant", "pig", "pigeon", "piranha", "polar_bear", "pufferfish", "puffin", "rabbit", "ram", "rhinoceros", "robin", "rooster", "sailfish", "sealion", "shark", "sheep", "sparrow", "squirrel", "stork", "swallow", "swan", "tapir", "triceratops", "tuna", "turkey", "unicorn", "walrus", "wolf"];

var pc =
	{
		pict_blob_info: null,
		pict_url: null,
		pict_ok: false,

		pict_from_me: [],
		pict_from_others: [],

		target_mat: null,
	};

function CommentObject(who, text) {
	this.text = text;
	this.who = who;
}
CommentObject.prototype.icon_src = function () {
	var hash = stringToHashCode(this.who);
	var icon = HEAD_ICONS[hash % HEAD_ICONS.length];
	return "../assets/icons/animals/" + icon + ".png";
};

function fileSizeHint(file_size) {
	if (file_size >= 1000 * 1000)
		return (file_size / 1024 / 1024).toPrecision(3) + " MB";
	else if (file_size >= 1000)
		return (file_size / 1024).toPrecision(3) + " KB";
	else
		return file_size + " Bytes";
}
function onQuestionToolMenu(elt) {
	var $dropdown = $("#qstn_tool_menu");
	var $elt = $(elt);

	var $sbjt = $elt.closest(".sbjt_c");
	pc.target_mat = $sbjt.attr("id");

	$elt.after($dropdown);
	//$elt.dropdown();		// The data-toggle="dropdown" will handle this.
}
function onAnswerToolMenu(elt) {
	var $dropdown = $("#ansr_tool_menu");
	var $elt = $(elt);

	var $ansr = $elt.closest(".ansr_c");
	pc.target_mat = $ansr.attr("id");

	var model = $ansr.data("ViewModel");
	var can_add = model.canAddPicture();

	$dropdown.find(".add_pict_li").toggleClass("disabled", !can_add);

	$elt.after($dropdown);
	//$elt.dropdown();		// The data-toggle="dropdown" will handle this.
}
function onAddPicture(elt) {
	var $elt = $(elt);
	var disabled = $elt.parent().hasClass("disabled");

	if (!disabled)
		$("#pictureDlg").modal('show');
}
function onOpenComment() {
	var $box = $("#enter_comment_box");
	var $comment_ta = $box.find("textarea");
	$comment_ta.val("");
	//autosize.update($comment_ta);		// not working when the textarea is hidden.

	var $target = $("#" + pc.target_mat);
	var $container = $target.find(".comment_cont");

	$container.append($box);
	setTimeout(function () {
		autosize.update($comment_ta);
		$comment_ta.focus();
	}, 0);
}
function onCommentDone() {
	var $box = $("#enter_comment_box");
	var $comment_ta = $box.find("textarea");
	var text = $comment_ta.val();

	if (text) {
		var cmmt_mat = randomAlphaNumericString(6);

		updateAction(gl.pageModel.pendingActions, cmmt_mat, text, "cmmt", pc.target_mat);
		submitActions(gl.pageModel.pendingActions);
	}
	var $container = $("#ecb_home");
	$container.append($box);
}
jQuery(document).ready(function () {
	loadRecentPicture();

	$('#pictureDlg').on('shown.bs.modal', function () {
		$('#pict_url_ipt').focus();
		pc.pict_ok = false;
		updateRecentPicture();
	}).on('hidden.bs.modal', function (e) {
		//if (!pc.pict_ok)
		//	restoreTool();
	});
	var $comment_ta = $("#enter_comment_box").find("textarea");
	$comment_ta.maxlength({ alwaysShow: true });
	autosize($comment_ta);

	var user_name = getUserName();
	var co = new CommentObject(user_name, "");
	$("#enter_comment_box").find("img")[0].src = co.icon_src();
});
function resetPictDialog(include_input) {
	$("#pict_info > mark:first").text("");
	$("#pict_info > mark:last").text("");
	$("#pict_info > strong").text("");
	$("#pict_error").text("");

	$("#pict_ok_btn").prop("disabled", true);
	$("#pict_preview_img").remove();

	if (include_input) {
		$("#pict_url_ipt").val("");
		$("#pict_file_chooser").val("");		// Triggers onPictChosen() with files.length == 0.

		pc.pict_blob_info = null;
		pc.pict_url = null;

		$("#my_recent_pict_tbl tr").removeClass("info");
		$("#others_pict_tbl tr").removeClass("info");
	}
}
function onPictRecent(url) {
	pc.pict_blob_info = null;
	$("#pict_file_chooser").val("");
	$("#pict_url_ipt").val("");

	loadPict(url, 0);
}
function onPictUrl() {
	pc.pict_blob_info = null;
	$("#pict_file_chooser").val("");
	$("#my_recent_pict_tbl tr").removeClass("info");
	$("#others_pict_tbl tr").removeClass("info");

	var url = $("#pict_url_ipt").val();
	loadPict(url, 0);
}
function loadPict(url, file_size) {
	resetPictDialog(false);

	pc.pict_url = null;

	var html = "<img id='pict_preview_img' class='img-responsive center-block img-thumbnail' />";
	var $elt = $(html);
	var image = $elt[0];

	image.onload = function (event) {
		var hint = file_size ? fileSizeHint(file_size) : "";

		$("#pict_info > strong").text(hint);
		$("#pict_info > mark:first").text(image.naturalWidth);
		$("#pict_info > mark:last").text(image.naturalHeight);

		if (!pc.pict_blob_info) {
			pc.pict_url = url;
			$("#pict_ok_btn").prop("disabled", false);
		}
		else {
			if (image.naturalWidth > 2000) {
				$("#pict_error").text("Image width (" + image.naturalWidth + ") too large. Only images with width <= 2000 are accepted.");
			}
			else if (image.naturalHeight > 2000) {
				$("#pict_error").text("Image height (" + image.naturalHeight + ") too large. Only images with height <= 2000 are accepted.");
			}
			else if (file_size > 2048 * 1024) {
				$("#pict_error").text("Image file size (" + hint + ") too large. Only files smaller than 2 MB are accepted.");
			}
			else
				$("#pict_ok_btn").prop("disabled", false);
		}
	};
	image.onerror = function (event) {
		$("#pict_error").text("Sorry, image loading failed.");
	};
	image.src = url;
	$("#pictureDlg .modal-body").prepend($elt);
}
function onPictChosen(files) {
	pc.pict_blob_info = null;

	if (files.length == 1) {
		$("#pict_url_ipt").val("");
		$("#my_recent_pict_tbl tr").removeClass("info");
		$("#others_pict_tbl tr").removeClass("info");

		var type = files[0].type;

		if (type.substring(0, 6) === "image/") {
			//pc.pict_blob = files[0];
			pc.pict_blob_info = { org_blob: files[0] };

			shrinkImage(pc.pict_blob_info);

			/*var getBlobURL = (window.URL && URL.createObjectURL.bind(URL)) ||
							(window.webkitURL && webkitURL.createObjectURL.bind(webkitURL)) ||
							window.createObjectURL;

			var pict_blob_url = getBlobURL(pc.pict_blob);*/

			loadPict(pc.pict_blob_info.org_blob_url, pc.pict_blob_info.org_blob.size);
		}
	}
}
function onImageClick(event) {
	var $img = $(event.target);
	var uri = $img.parent("a")[0].href;

	var window_name = hrefToWindowName(uri);
	window.open(uri, window_name);
	return false;
}
function insertPictAttachment(target_mat, uri) {
	var $target = $("#" + target_mat);
	var $container = $target.find(".atta_cont").first();

	var html = '<a class="thumbnail" target="_blank"><img></a>'
	var $elm = $(html);

	// The icheck onclick handler prevents anchor from being followed when click on img.
	$elm[0].href = uri;
	$elm.children("img").on("click", onImageClick)[0].src = toThumbnailUri(uri);

	$container.empty().prepend($elm);
}
function uploadPicturePath(uri) {
	//var picture_path = new Path([1, 0, 0, 1], 1, ly.active_layer, PT_PICTURE, g_user_id, ds.last_click_upt);
	//picture_path.uri = uri;

	//ua.wait_direct_upload_paths.push(picture_path);

	addRecentPicture(uri, true);
	//
	var $target = $("#" + pc.target_mat);
	insertPictAttachment(pc.target_mat, uri);

	var model = $target.data("ViewModel");
	model.updateAttachment(uri, "pict");
}
function toThumbnailUri(uri) {
	var thumbnail_uri = uri.replace("/n5/", "/n4/").replace("/n3/", "/n2/");
	return thumbnail_uri;
}
function uploadPicture() {
	var upload_files = new FormData();

	prepareUploadFile(pc.pict_blob_info, upload_files);

	//upload_files.append("picture", pict_blob);

	var post_data = {
		upload_files: upload_files,
	};
	sessionPost("/api/picture", post_data, function (suc, response) {
		if (suc && response.code == ResultSuccess && response.uri) {
			errorPrint("Upload image success. " + response.uri);
			uploadPicturePath(response.uri);
		}
		else {
			topAlert("抱歉，上傳圖片失敗。訊息：" + response + "。");
		}
	});
}
function addRecentPicture(url, from_me) {
	var list = from_me ? pc.pict_from_me : pc.pict_from_others;

	var idx = list.indexOf(url);
	if (idx != -1)
		list.splice(idx, 1);
	else if (list.length >= RECENT_PICT_COUNT)
		list.pop();

	list.unshift(url);

	if (from_me) {
		var text = pc.pict_from_me.join(",");
		localStorage.setItem("my_recent_pict", text);
	}
}
function loadRecentPicture() {
	var text = localStorage.getItem("my_recent_pict");
	if (text) {
		pc.pict_from_me = text.split(",");
	}
}
function updateRecentPicture() {
	var $tbl = $("#my_recent_pict_tbl");
	$tbl.empty();

	for (var i = 0; i < RECENT_PICT_COUNT; i++) {
		var url = pc.pict_from_me[i];
		if (url)
			insertRecentPicture($tbl, url, i);
	}

	$tbl = $("#others_pict_tbl");
	$tbl.empty();

	for (var i = 0; i < RECENT_PICT_COUNT; i++) {
		var url = pc.pict_from_others[i];
		if (url)
			insertRecentPicture($tbl, url, i);
	}
}
function insertRecentPicture($tbl, url, idx) {
	var html = "<tr onclick='onRecentPictClick(this)'><th></th><td class='rptnc'><img class='img-thumbnail' /></td><td><a target='_blank' class='text-info'></a></td></tr>";
	var $row = $(html);

	$row.children("th").text(idx + 1);
	$row.find("img")[0].src = url;
	$row.find("a").text(url)[0].href = url;

	$tbl.append($row);
}
function onRecentPictClick(elt) {
	var $row = $(elt);
	var url = $row.find("a")[0].href;

	$("#my_recent_pict_tbl tr").removeClass("info");
	$("#others_pict_tbl tr").removeClass("info");

	$row.addClass("info");

	onPictRecent(url);
}
function onPictureClick() {
	//removeCursorClass();

	//$("button.active").removeClass("active");
	//$("#picture_tool").addClass("active");

	//$("#avatar_canvas").addClass("cursor_picture");
	//refocus();
	pc.pict_ok = true;
	//
	setTimeout(function () {
		if (pc.pict_blob_info) {
			uploadPicture();
			resetPictDialog(true);
		}
		else if (pc.pict_url) {
			uploadPicturePath(pc.pict_url);
			resetPictDialog(true);
		}
	}, 0);		// set timeout so that dialog can be closed by ok button.
}
function shrinkImageInternal(info, image, max_width, max_height, prop_name, type) {
	var getBlobURL = (window.URL && URL.createObjectURL.bind(URL)) ||
		(window.webkitURL && webkitURL.createObjectURL.bind(webkitURL)) ||
		window.createObjectURL;

	var canvas = document.createElement("canvas");

	var ratio = Math.min(max_width / image.width, max_height / image.height, 1);
	var width = Math.round(image.width * ratio);
	var height = Math.round(image.height * ratio);

	canvas.width = width;
	canvas.height = height;
	canvas.getContext("2d").drawImage(image, 0, 0, width, height);

	// data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAAAAAAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKS...
	var data_url = canvas.toDataURL(type, 0.8);

	info[prop_name] = dataURLtoBlob(data_url, type);
	info[prop_name + "_url"] = getBlobURL(info[prop_name]);
}
function shrinkImage(info) {
	var getBlobURL = (window.URL && URL.createObjectURL.bind(URL)) ||
		(window.webkitURL && webkitURL.createObjectURL.bind(webkitURL)) ||
		window.createObjectURL;

	info.org_blob_url = getBlobURL(info.org_blob);

	var threshold = (info.org_blob.type === "image/gif" ? 500 : 250) * 1024;

	if (info.org_blob.size >= 30 * 1024) {
		var image = document.createElement("img");

		image.onload = function () {
			var type = info.org_blob.type === "image/png" ? "image/png" : "image/jpeg";

			if (info.org_blob.size >= threshold) {
				shrinkImageInternal(info, image, 1200, 900, "shrunk_blob", type);
				if (info.shrunk_blob.size > info.org_blob.size) {
					delete info.shrunk_blob;
					delete info.shrunk_blob_url;
				}
			}
			shrinkImageInternal(info, image, 300, 300, "thumbnail_blob", type);
			if (info.thumbnail_blob.size > info.org_blob.size) {
				delete info.thumbnail_blob;
				delete info.thumbnail_blob_url;
			}
		};
		image.src = info.org_blob_url;
	}
}
function prepareUploadFile(info, upload_files) {
	var key = "picture";

	if (!info.thumbnail_blob) {
		if (info.shrunk_blob)
			upload_files.append("n6/" + key, info.shrunk_blob);
		else
			upload_files.append("n1/" + key, info.org_blob);
	}
	else {
		if (info.shrunk_blob) {
			upload_files.append("n5/" + key, info.shrunk_blob);
			upload_files.append("n4/" + key, info.thumbnail_blob);
		}
		else {
			upload_files.append("n3/" + key, info.org_blob);
			upload_files.append("n2/" + key, info.thumbnail_blob);
		}
	}
}
function dataURLtoBlob(data_url, type) {
	var pos = data_url.indexOf(",");

	var bin_str = atob(data_url.substr(pos + 1));

	var ab = new ArrayBuffer(bin_str.length);
	var view = new Uint8Array(ab);

	for (var i = 0; i < view.length; i++) {
		view[i] = bin_str.charCodeAt(i);
	}
	return new Blob([ab], { type: type/*"image/jpeg"*/ });
	//var bb = new BlobBuilder();
	//bb.append(ab);
	//return bb.getBlob("image/jpeg");
};