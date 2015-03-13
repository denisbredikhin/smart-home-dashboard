<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Thermostats.aspx.cs" Inherits="HomeDashboard.Thermostats" %>
<%@ Register TagPrefix="asp" Namespace="System.Web.UI.HtmlControls" Assembly="System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Температура дома</title>
    <link href='//fonts.googleapis.com/css?family=Open+Sans&subset=latin,cyrillic' rel='stylesheet' type='text/css' />
    <script type='text/javascript' src='//code.jquery.com/jquery-1.9.1.js'></script>
    <script type='text/javascript' src='//cdn.jsdelivr.net/rangeslider.js/0.3.7/rangeslider.js'></script>
    <link href='Styles/Common.css' rel='stylesheet' type='text/css' />
    <link href='//cdn.jsdelivr.net/rangeslider.js/0.3.7/rangeslider.css' rel='stylesheet' type='text/css' />
    <script type="text/javascript">
        $("input[type=range]").bind("onchange", function (value) {
            alert(value);
        });

        $('input[type=range]').rangeslider();
        setInterval(function () {
            UpdateData();
        }, 60000);

        function UpdateData()
        {
            $.get("../ThermostatsData.hnd", function (data) {

                $("tr.dataRow").each(function () {
                    $this = $(this);
                    var deviceName = $this.find("td.deviceName").html();
                    var deviceTemp = $this.find("td.deviceTemp").html();
                    var deviceBattery = $this.find("div.deviceBattery").html();

                    var newTemp;
                    var newBattery;

                    data.forEach(function (entry) {
                        var deviceNameNew = entry.DeviceName;
                        var valueName = entry.ValueName;
                        var val = entry.Value;

                        if (deviceName == deviceNameNew) {
                            if (valueName == 'Heating 1')
                                newTemp = val;
                            else if (valueName == 'Battery Level')
                                newBattery = val;
                        }
                    });

                    $this.find("td.deviceTemp").html(newTemp + "&deg;C");
                    var batteryDiv = $this.find("div.deviceBattery");
                    batteryDiv.html(newBattery + "%");
                    batteryDiv.width(newBattery + "%");
                });
            });
        }

    </script>
    <style>
        .datagrid table 
        { border-collapse: collapse; text-align: left; width: 100%; } 
        .datagrid 
        {font-family: 'Open Sans', sans-serif; background: #fff; overflow: hidden; border: 1px solid #006699; -webkit-border-radius: 3px; -moz-border-radius: 3px; border-radius: 3px; }
        .datagrid table td, .datagrid table th { padding: 3px 10px; }
        .datagrid table thead th 
        {background:-webkit-gradient( linear, left top, left bottom, color-stop(0.05, #006699), color-stop(1, #00557F) );background:-moz-linear-gradient( center top, #006699 5%, #00557F 100% );filter:progid:DXImageTransform.Microsoft.gradient(startColorstr='#006699', endColorstr='#00557F');background-color:#006699; color:#FFFFFF; font-size: 35px; font-weight: bold; border-left: 1px solid #0070A8; } 
        .datagrid table thead th:first-child { border: none; }
        .datagrid table tbody td { color: #00496B; border-left: 1px solid #E1EEF4;font-size: 35px;font-weight: normal; }
        .datagrid table tbody tr:nth-child(even) { background: #E1EEF4; color: #00496B; }
        .datagrid table tbody td:first-child { border-left: none; }
        .datagrid table tbody tr:last-child td { border-bottom: none; }
    </style>
    <style>
        #batteryBody {
            float: left;
            width: 200px;
            height: 60px;
            border: 10px #CCC solid;
            margin-bottom: 7px;
        }
        .container {
            width: 240px;
            margin-left: auto;
            margin-right: auto;
            margin-top: 7px;
        }
        .batteryEnd {
            float: left;
            height: 80px;
            width: 15px;
        }
        .batteryEnd div {
            width: 15px;
            height: 35px;
            margin-top: 25px;
            margin-left: 5px;
            background-color: #CCC;
        }
        #indicator {
            height: 100%;
            width: 50%;
            background-color: #FFFF00;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="datagrid">
            <table>
                <thead>
                    <tr>
                        <th>Термостат</th>
                        <th>Температура</th>
                        <th>Батарея</th>
                    </tr>
                </thead>
                <tbody>
                    <%=tableHtml%>
                </tbody>
            </table>
        </div>
        <div style="text-align:center;margin-top:20px;width:100%">
            <a class="btn" href="Menu.html">Назад</a>
            &nbsp;
            <a class="btn" href="#" onclick="UpdateData(); return false;">Обновить</a>
        </div>
        
    </form>
</body>
</html>
