using System.Drawing;
using System.Windows.Forms;
namespace GarageGestion {
 public sealed class MainForm : Form {
  public MainForm() {
   Text = "Garage Gestion"; StartPosition = FormStartPosition.CenterScreen;
   MinimumSize = new Size(900, 600); Size = new Size(1100, 720);
   BackColor = Color.FromArgb(245,247,250); Font = new Font("Segoe UI", 9F);
   var header = new Panel { Dock=DockStyle.Top, Height=76, BackColor=Color.FromArgb(29,48,73) };
   header.Controls.Add(new Label { Text="GARAGE GESTION", ForeColor=Color.White, Font=new Font("Segoe UI",19F,FontStyle.Bold), AutoSize=true, Location=new Point(24,19) });
   header.Controls.Add(new Label { Text="Licensed by Aymen AHMED ESSALAH", ForeColor=Color.Gainsboro, AutoSize=true, Location=new Point(790,30) });
   var menu = new FlowLayoutPanel { Dock=DockStyle.Left, Width=220, Padding=new Padding(14), FlowDirection=FlowDirection.TopDown, WrapContents=false, BackColor=Color.FromArgb(235,239,244) };
   string[] entries = { "Tableau de bord","Clients","Véhicules","Ordres de réparation","Devis","Factures","Fournisseurs","Pièces et stock","Paiements","Documents PDF" };
   foreach (var entry in entries) {
    var b = new Button { Text=entry, Width=185, Height=38, TextAlign=ContentAlignment.MiddleLeft, FlatStyle=FlatStyle.Flat, BackColor=Color.White, Margin=new Padding(0,0,0,8), Padding=new Padding(10,0,0,0) };
    b.Click += delegate { MessageBox.Show("Module en préparation : " + entry, "Garage Gestion"); };
    menu.Controls.Add(b);
   }
   var content = new Panel { Dock=DockStyle.Fill, Padding=new Padding(30) };
   content.Controls.Add(new Label { Text="Bienvenue dans Garage Gestion", Font=new Font("Segoe UI",22F,FontStyle.Bold), ForeColor=Color.FromArgb(29,48,73), AutoSize=true, Location=new Point(30,35) });
   content.Controls.Add(new Label { Text="Prototype initial — interface française\r\n\r\nLes modules seront activés progressivement : clients, véhicules, atelier, devis, facturation, fournisseurs, stock et comptabilité.", Font=new Font("Segoe UI",11F), ForeColor=Color.DimGray, AutoSize=true, Location=new Point(34,95) });
   Controls.Add(content); Controls.Add(menu); Controls.Add(header);
  }
 }
}