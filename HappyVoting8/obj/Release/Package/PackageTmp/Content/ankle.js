var g_user_name;
var g_user_pwd_token;

var ResultSuccess = 0;
var ResultInvalidLoginToken = -1;
var ResultInvalidUserName = -2;
var ResultWrongPassword = -3;
var ResultInvalidTempKey = -4;
var ResultUserNameOccupied = -5;
var ResultUserDoesNotExist = -6;
var ResultInvalidSession = -7;

function getUserName() {
	if (!g_user_name) {
		if (!localStorage["g_user_name"])
			localStorage["g_user_name"] = "_" + randomAlphaNumericString(8);

		g_user_name = localStorage["g_user_name"];
	}
	return g_user_name;
}
function passwordToToken(password) {
	var t = password + "hopeless";
	var wa = CryptoJS.MD5(t);
	var token = wa.toString(CryptoJS.enc.Hex);

	return token;
}
function getUserPasswordToken() {
	if (!g_user_pwd_token) {
		if (!localStorage["g_user_pwd_token"])
			localStorage["g_user_pwd_token"] = passwordToToken(randomAlphaNumericString(8));

		g_user_pwd_token = localStorage["g_user_pwd_token"];
	}
	return g_user_pwd_token;
}
function userRegister(callback) {
	var user_name = getUserName();
	var pwd_token = getUserPasswordToken();

	postGoods("/api/register",
		{
			user_name: user_name,
			pwd_token: pwd_token,
		},
		function (suc, response) {
			if (suc && response.code == ResultSuccess) {
				console.log("Register succeeded. user_name=" + user_name + ".");

				localStorage["g_session_key"] = response.session_key;
				callback(true);
			}
			else {
				console.log("Register failed. user_name=" + user_name + ".");
				callback(false);
			}
		}
	);
}
function userLogin(callback) {
	postGoods("/api/gettempkey",
		{
		},
		function (suc, response) {
			if (suc && response.code == ResultSuccess && response.temp_key) {
				doLogin(response.temp_key, callback);
			}
			else {
				console.log("Get temp key failed.");
				callback(false);
			}
		}
	);
}
function doLogin(temp_key, callback) {
	var user_name = getUserName();
	var pwd_token = getUserPasswordToken();

	var pwd_hash = CryptoJS.MD5(pwd_token + temp_key).toString(CryptoJS.enc.Hex);

	postGoods("/api/login",
		{
			user_name: user_name,
			temp_key: temp_key,
			pwd_hash: pwd_hash,
		},
		function (suc, response) {
			if (suc) {
				if (response.code == ResultSuccess) {
					console.log("Login succeeded. user_name=" + user_name + ".");

					localStorage["g_session_key"] = response.session_key;
					callback(true);
				}
				else if (response.code == ResultUserDoesNotExist) {
					console.log("User does not exist. Start register. user_name=" + user_name + ".");
					userRegister(callback);
				}
				else {
					console.log("Login failed. user_name=" + user_name + ".");
					callback(false);
				}
			}
			else {
				callback(false);
			}
		}
	);
}
function sessionPost(url, post_data, callback) {
	if (!localStorage["g_session_key"])
		userLogin(function (suc) {
			if (suc)
				sessionPostInternal(url, post_data, callback);
			else
				callback(false, "Login procedure failed.");
		});
	else
		sessionPostInternal(url, post_data, callback);
}
function sessionPostInternal(url, post_data, callback) {
	post_data.session_key = localStorage["g_session_key"];

	postGoods(url, post_data, function (suc, response) {
		if (suc)
			if (response.code == ResultInvalidSession) {
				console.log("Session invalid. Start login.");

				localStorage.removeItem("g_session_key");
				sessionPost(url, post_data, callback);
			}
			else {
				console.log("Session post succeeded. url=" + url + ".");
				callback(true, response);
			}
		else {
			console.log("Session post failed. reason=" + response);
			callback(false, response);
		}
	});
}
function sessionGet(url, params, callback) {
	if (!localStorage["g_session_key"])
		userLogin(function (suc) {
			if (suc)
				sessionGetInternal(url, params, callback);
			else
				callback(false, "Login procedure failed.");
		});
	else
		sessionGetInternal(url, params, callback);
}
function sessionGetInternal(url, params, callback) {
	params.session_key = localStorage["g_session_key"];

	var query = $.param(params);

	getGoods(url + "?" + query, function (suc, response) {
		if (suc)
			if (response.code == ResultInvalidSession) {
				console.log("Session invalid. Start login.");

				localStorage.removeItem("g_session_key");
				sessionGet(url, params, callback);
			}
			else {
				console.log("Session get succeeded. url=" + url + ".");
				callback(true, response);
			}
		else {
			console.log("Session get failed. reason=" + response);
			callback(false, response);
		}
	});
}
function simpleGet(url, params, callback) {
	var query = $.param(params);

	getGoods(url + "?" + query, callback);
}
function unitTest() {
	/*sessionPost("/api/test", {}, function (suc, response) {
		if (suc && response.code == ResultSuccess) {
		}
		else {
		}
	});*/
	//userLogin();
	//userRegister();

	/*var user_name = getUserName();
	var pwd_token = getUserPasswordToken();

	var user_name = getUserName();
	var pwd_token = getUserPasswordToken();*/
}
