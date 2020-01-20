var rt =
	{
		charts: {},
	};
function initPieChart(chart_id) {
	var chart_obj = AmCharts.makeChart(chart_id, {
		"type": "pie",
		"theme": "light",

		"fontFamily": "'Open Sans', Helvetica, Arial, STHeiti, 'Microsoft JhengHei', 微軟正黑體, sans-serif",
		"color": '#888',

		"dataProvider": [],
		"valueField": "ansc",
		"titleField": "anst",
		"outlineAlpha": 0.4,
		"depth3D": 15,
		"balloonText": "[[title]]<br><span style='font-size:14px'><b>[[value]]</b> ([[percents]]%)</span>",
		"angle": 30,
		"exportConfig": {
			menuItems: [{
				icon: App.getGlobalPluginsPath() + "amcharts/amcharts/images/export.png",
				format: 'png'
			}]
		},
		"labelRadius": 30,
		"fontSize": 12,
		"colorField": "color",
		"creditsPosition": "top-right",

		maxLabelWidth: 80,
		/*autoMargins: false,
		marginTop: 0,
		marginBottom: 0,
		marginLeft: 0,
		marginRight: 0,*/
		pullOutRadius: "15%",
	});
	return chart_obj;
};
function initLineChart(chart_id) {
	var chart_obj = AmCharts.makeChart(chart_id, {
		"theme": "light",
		"type": "serial",
		"startDuration": 2,

		"fontFamily": "'Open Sans', Helvetica, Arial, STHeiti, 'Microsoft JhengHei', 微軟正黑體, sans-serif",
		"color": '#888',

		"dataProvider": [],
		"valueAxes": [{
			"position": "left",
			"axisAlpha": 0,
			"gridAlpha": 0,
			"title": "票數",
			"precision": 0,
		}],
		"graphs": [{
			"balloonText": "[[category]]: <b>[[value]]</b>",
			"colorField": "color",
			"fillAlphas": 0.85,
			"lineAlpha": 0.1,
			"type": "column",
			"topRadius": 1,
			"valueField": "ansc",
			"labelText": "[[value]]",
			"labelOffset": 16,
		}],
		"depth3D": 40,
		"angle": 30,
		"chartCursor": {
			"categoryBalloonEnabled": false,
			"cursorAlpha": 0,
			"zoomable": false
		},
		"categoryField": "anst",
		"categoryAxis": {
			"gridPosition": "start",
			"axisAlpha": 0,
			"gridAlpha": 0,
			"labelRotation": 45,
		},
		"exportConfig": {
			"menuTop": "20px",
			"menuRight": "20px",
			"menuItems": [{
				"icon": '/lib/3/images/export.png',
				"format": 'png'
			}]
		},
		"fontSize": 12,
		"creditsPosition": "top-right",
	}, 0);
	return chart_obj;
};
function initCharts($sbjt) {
	var sbjt_mat = $sbjt.attr("id");

	var $chart = $sbjt.find(".chart");
	$chart[0].id = randomAlphaNumericString(6);//"piec_" + next_question_id;
	$chart[1].id = randomAlphaNumericString(6);//"linc_" + next_question_id;

	$chart[0].parentNode.id = randomAlphaNumericString(6);//"piec_tab_" + next_question_id;
	$chart[1].parentNode.id = randomAlphaNumericString(6);//"linc_tab_" + next_question_id;

	var $tabs = $sbjt.find("[data-toggle='tab']");
	$tabs[0].href = "#" + $chart[0].parentNode.id;
	$tabs[1].href = "#" + $chart[1].parentNode.id;

	var pie_chart_obj = initPieChart($chart[0].id);
	var line_chart_obj = initLineChart($chart[1].id);

	rt.charts[sbjt_mat] = [pie_chart_obj, line_chart_obj];
}
function updateCharts(sbjt_mat, data) {
	rt.charts[sbjt_mat].forEach(function (chart_obj) {
		chart_obj.dataProvider = data;
		chart_obj.validateData();
	});
}
function deleteCharts(sbjt_mat) {
	delete rt.charts[sbjt_mat];
}