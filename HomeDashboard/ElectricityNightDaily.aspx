﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ElectricityNightDaily.aspx.cs" Inherits="HomeDashboard.ElectricityNightDaily" %>

<%@ Register src="MeterDaily.ascx" tagname="MeterDaily" tagprefix="uc1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Расход ночного электричества по дням</title>
    <link href='Styles/Common.css' rel='stylesheet' type='text/css' />
</head>
<body>
    <form id="form1" runat="server">
        
        <uc1:MeterDaily ID="electrNightDailyControl" ColumnName="ElectricityNightDiff" Title="Расход ночного электричества по дням" TitleYAxis="Расход, квт-ч" SeriesName="Расход электричества" runat="server" />
        <div style="text-align:center;margin-top:20px;width:100%">
         <a class="btn" href="Menu.html">Назад</a>
        </div>
    </form>
</body>
</html>
