<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MetersRegistration.aspx.cs" Inherits="HomeDashboard.MetersRegistration" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Регистрация показаний счетчиков</title>
    <link href='Styles/Common.css' rel='stylesheet' type='text/css' />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Газ:        
    </div>
    <asp:TextBox runat="server" ID="gas"></asp:TextBox>
    
    <div>
        Эл-во день:        
    </div>
    <asp:TextBox runat="server" ID="electrDay"></asp:TextBox>
    
    <div>
        Эл-во ночь:        
    </div>
    <asp:TextBox runat="server" ID="electrNight"></asp:TextBox>
    
    <div>
        Вода:        
    </div>
    <asp:TextBox runat="server" ID="water"></asp:TextBox>
    
    <br><br>
        <asp:Button runat="server" OnClick="okButton_Click" ID="okButton" Text="Зарегистрировать" />
        
        <div style="text-align:center;margin-top:20px;width:100%">
         <a class="btn" href="Menu.html">Назад</a>
        </div>
    </form>
</body>
</html>
