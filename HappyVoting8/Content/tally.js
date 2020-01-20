var tl =
	{
		tallyDp: {},
	};
function Tally(sbjt_mat, ansr_mat, default_text) {
	this.ansc = 0;
	this.anst = default_text;
	this.color = stringToColor(this.anst);

	this.default_text = default_text;
	this.ansr_mat = ansr_mat;
	this.sbjt_mat = sbjt_mat;
}
Tally.prototype.modifyText = function (text) {
	this.anst = text.length > 0 ? text : this.default_text;
	this.color = stringToColor(this.anst);

	updateCharts(this.sbjt_mat, tl.tallyDp[this.sbjt_mat]);
}
Tally.prototype.setTally = function (tpt) {
	this.ansc = tpt / TALLY_POINT_PRECISION;
	updateCharts(this.sbjt_mat, tl.tallyDp[this.sbjt_mat]);
}
function createTally(sbjt_mat, ansr_mat, default_text) {
	if (!tl.tallyDp[sbjt_mat])
		tl.tallyDp[sbjt_mat] = [];

	var tally_obj = new Tally(sbjt_mat, ansr_mat, default_text);
	tl.tallyDp[sbjt_mat].push(tally_obj);

	return tally_obj;
}
function deleteTally(tally_obj) {
	var arr = tl.tallyDp[tally_obj.sbjt_mat];
	if (arr) {
		var i = arr.indexOf(tally_obj);
		if (i > -1) {
			arr.splice(i, 1);
			updateCharts(tally_obj.sbjt_mat, arr);
		}
	}
}