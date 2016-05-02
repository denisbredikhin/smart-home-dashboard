<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PriceAllDaily.aspx.cs" Inherits="HomeDashboard.PriceAllDaily" %>


<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Расход по дням, центов</title>
    <link href='Styles/Common.css' rel='stylesheet' type='text/css' />
    <script type='text/javascript' src='//code.jquery.com/jquery-1.9.1.js'></script>
    <script src="http://code.highcharts.com/stock/highstock.js"></script>
    <script src="http://code.highcharts.com/stock/modules/exporting.js"></script>
</head>
<body>
    <form id="form1" runat="server">
            
        <div id="container" style="min-width: 310px; height: 400px; margin: 0 auto"></div>

<script type="text/javascript">
    var highchartsOptions = Highcharts.setOptions({
        lang: {
            loading: 'Загрузка...',
            months: ['Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь', 'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'],
            weekdays: ['Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота', 'Воскресенье'],
            shortMonths: ['Янв', 'Фев', 'Март', 'Апр', 'Май', 'Июнь', 'Июль', 'Авг', 'Сент', 'Окт', 'Нояб', 'Дек'],
            exportButtonTitle: "Экспорт",
            printButtonTitle: "Печать",
            rangeSelectorFrom: "От",
            rangeSelectorTo: "До",
            rangeSelectorZoom: "Период",
            downloadPNG: 'Скачать PNG',
            downloadJPEG: 'Скачать JPEG',
            downloadPDF: 'Скачать PDF',
            downloadSVG: 'Скачать SVG',
            resetZoom: "Сбросить зум",
            resetZoomTitle: "Сбросить зум",
            thousandsSep: " ",
            decimalPoint: ','
        }
    }
  );

    $(function () {
        $('#container').highcharts('StockChart', {
            chart: {
                zoomType: 'x'
            },
            title: {
                text: 'Расход по дням, центов'
            },
            subtitle: {
                text: document.ontouchstart === undefined ?
                    'Click and drag in the plot area to zoom in' :
                    'Pinch the chart to zoom in'
            },
            xAxis: {
                type: 'datetime',
                minRange: 14 * 24 * 3600000 // fourteen days
            },
            yAxis: {
                title: {
                    text: 'Расход, центов'
                },
                min: 0
            },
            legend: {
                enabled: false
            },

            rangeSelector : {
                selected : 1,
                inputEnabled: $('#container').width() > 480
            },

            series: [{
                name: 'Газ',
                id: 'dataseries1',
                pointInterval: 24 * 3600 * 1000,
                pointStart: Date.UTC(<%=minYear%>, <%=minMonth%>,<%=minDay%>),
                data: [
                    <%=chartDataGas%>
                ]
            },
            {
                name: 'Эл-во',
                id: 'dataseries2',
                pointInterval: 24 * 3600 * 1000,
                pointStart: Date.UTC(<%=minYear%>, <%=minMonth%>,<%=minDay%>),
                data: [
                    <%=chartDataElectricity%>
                ]
            },
            {
                 name: 'Вода',
                 id: 'dataseries3',
                 pointInterval: 24 * 3600 * 1000,
                 pointStart: Date.UTC(<%=minYear%>, <%=minMonth%>,<%=minDay%>),
                data: [
                    <%=chartDataWater%>
                ]
             },
            // the event marker flags
			{
			    type : 'flags',
			    data : [<%=flagsData%>],
            onSeries : 'dataseries',
            shape : 'circlepin',
            width : 16
			}]
        });
    });

</script>
        <div style="text-align:center;margin-top:20px;width:100%">
         <a class="btn" href="Menu.html">Назад</a>
        </div>
    </form>
</body>
</html>
