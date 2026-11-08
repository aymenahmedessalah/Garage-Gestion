using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GarageGestion {
 public sealed class MainForm : Form {
  sealed class Field { public string Key; public string Label; public Field(string k,string l){Key=k;Label=l;} }
  sealed class Module { public string Name; public string[] Fields; public Module(string n, params string[] f){Name=n;Fields=f;} }
  sealed class Store { public string Company="Mon Garage"; public string Currency="TND"; public string Currencies="TND, EUR, USD"; public string Logo=""; public Dictionary<string,List<Dictionary<string,string>>> Data=new Dictionary<string,List<Dictionary<string,string>>>(); }
  readonly string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GarageGestion");
  readonly string dbFile;
  readonly JavaScriptSerializer json=new JavaScriptSerializer();
  Store store;
  readonly Dictionary<string,Module> modules=new Dictionary<string,Module>();
  readonly Dictionary<string,TabPage> opened=new Dictionary<string,TabPage>();
  Panel sidebar, header, workspace;
  Button menuToggle;
  Label companyLabel;
  TabControl tabs;
  bool sidebarExpanded=true;
  Timer slideTimer;
  int targetWidth=238;
  readonly Color navy=Color.FromArgb(26,42,64), blue=Color.FromArgb(42,112,190), canvas=Color.FromArgb(244,247,251), ink=Color.FromArgb(39,53,72);
  public MainForm(){
   dbFile=Path.Combine(root,"garage-data.json"); Directory.CreateDirectory(root); LoadStore(); DefineModules();
   Text="Garage Gestion"; StartPosition=FormStartPosition.CenterScreen; MinimumSize=new Size(980,650); Size=new Size(1280,820); BackColor=canvas; Font=new Font("Segoe UI",9F); DoubleBuffered=true;
   BuildShell(); OpenDashboard(); Shown+=delegate{ if(String.IsNullOrWhiteSpace(store.Company)||store.Company=="Mon Garage") OpenModule("Paramètres"); };
  }
  void DefineModules(){
   Add("Clients","Nom","Téléphone","E-mail","Adresse","Identifiant fiscal","Notes");
   Add("Véhicules","Client","Marque","Modèle","Immatriculation","VIN","Motorisation","Kilométrage");
   Add("Ordres de réparation","N° OR","Client","Véhicule","Date entrée","Kilométrage entrée","Symptômes","Diagnostic","Travaux","Statut");
   Add("Devis","N° devis","Date","Client","Véhicule","Désignation","Heures MO","Tarif horaire","Pièces HT","Remise","TVA %","Total");
   Add("Factures","N° facture","Date","Client","Véhicule","Désignation","Heures MO","Tarif horaire","Pièces HT","Remise","TVA %","Total","Statut");
   Add("Fournisseurs","Nom","Contact","Téléphone","E-mail","Adresse","Conditions paiement","Délai livraison");
   Add("Pièces et stock","Référence","Désignation","Fournisseur","Quantité","Seuil alerte","Coût achat","Prix vente","Emplacement");
   Add("Paiements","Date","Type","Client/Fournisseur","Document","Montant","Mode","Référence");
   Add("Employés","Nom","Fonction","Téléphone","E-mail","Taux horaire","Date embauche","Actif");
   Add("Heures de travail","Employé","Date","Heure début","Heure fin","Pause (h)","Heures nettes","Taux horaire","Coût main-d'œuvre","Ordre de réparation");
   Add("Loyer et charges","Type","Bénéficiaire","Montant","Périodicité","Date échéance","Date paiement","Statut","Notes");
   Add("Documents PDF","Type document","Numéro","Date","Client","Montant","Fichier","Notes");
   Add("Paramètres","Nom de l'entreprise","Devise principale","Devises disponibles","Adresse","Téléphone","E-mail","Matricule fiscal","Taux TVA par défaut");
  }
  void Add(string n,params string[] f){modules[n]=new Module(n,f); if(!store.Data.ContainsKey(n))store.Data[n]=new List<Dictionary<string,string>>();}
  void LoadStore(){try{if(File.Exists(dbFile))store=json.Deserialize<Store>(File.ReadAllText(dbFile));}catch{} if(store==null)store=new Store(); if(store.Data==null)store.Data=new Dictionary<string,List<Dictionary<string,string>>>();}
  void SaveStore(){try{Directory.CreateDirectory(root);File.WriteAllText(dbFile,json.Serialize(store));}catch(Exception ex){MessageBox.Show("Erreur d'enregistrement : "+ex.Message);}}
  void BuildShell(){
   header=new Panel{Dock=DockStyle.Top,Height=66,BackColor=navy}; Controls.Add(header);
   menuToggle=MakeButton("☰",42,38);menuToggle.Location=new Point(12,13);menuToggle.Click+=delegate{sidebarExpanded=!sidebarExpanded;targetWidth=sidebarExpanded?238:68;slideTimer.Start();};header.Controls.Add(menuToggle);
   companyLabel=new Label{Text=store.Company,ForeColor=Color.White,Font=new Font("Segoe UI",15,FontStyle.Bold),AutoSize=true,Location=new Point(68,18)};header.Controls.Add(companyLabel);
   var sub=new Label{Text="GESTION INTÉGRÉE DE GARAGE",ForeColor=Color.FromArgb(177,193,211),AutoSize=true,Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(900,25)};header.Controls.Add(sub);header.Resize+=delegate{sub.Left=header.ClientSize.Width-sub.Width-22;};
   sidebar=new Panel{Dock=DockStyle.Left,Width=238,BackColor=Color.FromArgb(235,240,247),Padding=new Padding(8,12,8,8)};Controls.Add(sidebar);
   var nav=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.Transparent};sidebar.Controls.Add(nav);
   string[] names={"Tableau de bord","Clients","Véhicules","Ordres de réparation","Devis","Factures","Fournisseurs","Pièces et stock","Paiements","Employés","Heures de travail","Loyer et charges","Documents PDF","Paramètres"};
   foreach(string name in names){string captured=name;Button b=MakeButton("  "+IconFor(name)+"   "+name,214,40);b.TextAlign=ContentAlignment.MiddleLeft;b.Margin=new Padding(0,0,0,5);b.Click+=delegate{if(captured=="Tableau de bord")OpenDashboard();else OpenModule(captured);};nav.Controls.Add(b);b.Tag=name;}
   workspace=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12),BackColor=canvas};Controls.Add(workspace);
   tabs=new TabControl{Dock=DockStyle.Fill,Font=new Font("Segoe UI",9F),Padding=new Point(14,5)};tabs.SelectedIndexChanged+=delegate{UpdateTabVisibility();};workspace.Controls.Add(tabs);
   slideTimer=new Timer{Interval=12};slideTimer.Tick+=delegate{int diff=targetWidth-sidebar.Width;if(Math.Abs(diff)<=12){sidebar.Width=targetWidth;slideTimer.Stop();}else sidebar.Width+=diff/3;};
   sidebar.BringToFront();header.BringToFront();
  }
  string IconFor(string n){switch(n){case "Tableau de bord":return "▦";case "Clients":return "♙";case "Véhicules":return "▣";case "Ordres de réparation":return "⚙";case "Devis":return "≡";case "Factures":return "▤";case "Fournisseurs":return "♧";case "Pièces et stock":return "▦";case "Paiements":return "€";case "Employés":return "♟";case "Heures de travail":return "◷";case "Loyer et charges":return "⌂";case "Documents PDF":return "▧";default:return "⚙";}}
  Button MakeButton(string t,int w,int h){return new Button{Text=t,Width=w,Height=h,FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=ink,Font=new Font("Segoe UI",9F),Cursor=Cursors.Hand,FlatAppearance={BorderColor=Color.FromArgb(222,229,237),BorderSize=1}};}
  void UpdateTabVisibility(){ }
  void OpenDashboard(){OpenTab("Tableau de bord",BuildDashboard());}
  void OpenModule(string name){if(!modules.ContainsKey(name))return;OpenTab(name,BuildModulePage(name));}
  void OpenTab(string name,Control page){
   if(opened.ContainsKey(name)){tabs.SelectedTab=opened[name];page.Dispose();return;}
   var tab=new TabPage(name){BackColor=canvas};page.Dock=DockStyle.Fill;tab.Controls.Add(page);
   opened[name]=tab;tabs.TabPages.Add(tab);tabs.SelectedTab=tab;
  }
  Control BuildDashboard(){
   var p=new Panel{BackColor=canvas,Padding=new Padding(12),AutoScroll=true};
   var title=new Label{Text="Tableau de bord",Font=new Font("Segoe UI",22,FontStyle.Bold),ForeColor=ink,AutoSize=true,Location=new Point(20,15)};p.Controls.Add(title);
   var date=new Label{Text=DateTime.Now.ToString("dddd dd MMMM yyyy",new CultureInfo("fr-FR")),ForeColor=Color.Gray,AutoSize=true,Location=new Point(24,58)};p.Controls.Add(date);
   decimal revenue=Sum("Factures","Total"), expenses=Sum("Loyer et charges","Montant", "Statut","Payé")+Sum("Paiements","Montant","Type","Dépense");
   decimal unpaid=Sum("Factures","Total","Statut","En attente");
   AddMetric(p,"Chiffre d'affaires",Money(revenue),blue,20,100);
   AddMetric(p,"Charges enregistrées",Money(expenses),Color.FromArgb(208,125,58),300,100);
   AddMetric(p,"Factures en attente",Money(unpaid),Color.FromArgb(130,91,173),580,100);
   AddMetric(p,"Clients",store.Data["Clients"].Count.ToString(),Color.FromArgb(48,151,122),860,100);
   var chart=new ChartPanel{Location=new Point(20,235),Size=new Size(760,330),BackColor=Color.White,Values=MonthlyRevenue(),Title="Évolution du chiffre d'affaires"};p.Controls.Add(chart);
   var quick=new Panel{Location=new Point(805,235),Size=new Size(310,330),BackColor=Color.White,Padding=new Padding(18)};p.Controls.Add(quick);
   quick.Controls.Add(new Label{Text="Accès rapide",Font=new Font("Segoe UI",14,FontStyle.Bold),ForeColor=ink,AutoSize=true,Location=new Point(18,16)});
   string[] q={"Nouvelle facture","Nouveau client","Nouvel ordre de réparation","Saisir les heures","Ajouter une charge / loyer"};
   string[] dest={"Factures","Clients","Ordres de réparation","Heures de travail","Loyer et charges"};
   for(int i=0;i<q.Length;i++){int k=i;var b=MakeButton(q[i],265,39);b.Location=new Point(18,58+i*49);b.Click+=delegate{OpenModule(dest[k]);};quick.Controls.Add(b);}
   var note=new Label{Text="Les données sont enregistrées localement sur cet ordinateur.",ForeColor=Color.Gray,AutoSize=true,Location=new Point(22,590)};p.Controls.Add(note);
   return p;
  }
  void AddMetric(Control parent,string caption,string value,Color accent,int x,int y){
   var card=new Panel{Location=new Point(x,y),Size=new Size(250,112),BackColor=Color.White};parent.Controls.Add(card);
   card.Controls.Add(new Panel{Dock=DockStyle.Left,Width=5,BackColor=accent});
   card.Controls.Add(new Label{Text=caption,Location=new Point(18,17),AutoSize=true,ForeColor=Color.Gray,Font=new Font("Segoe UI",9)});
   card.Controls.Add(new Label{Text=value,Location=new Point(18,45),AutoSize=true,ForeColor=ink,Font=new Font("Segoe UI",19,FontStyle.Bold)});
  }
  decimal Sum(string module,string field,string filter="",string equals=""){decimal total=0;if(!store.Data.ContainsKey(module))return 0;foreach(var r in store.Data[module]){string v;if(filter!=""&&(!r.TryGetValue(filter,out v)||v!=equals))continue;if(r.TryGetValue(field,out v)){decimal n;if(decimal.TryParse(v,NumberStyles.Any,CultureInfo.InvariantCulture,out n))total+=n;}}return total;}
  string Money(decimal n){return n.ToString("N2",CultureInfo.GetCultureInfo("fr-FR"))+" "+store.Currency;}
  decimal[] MonthlyRevenue(){var a=new decimal[12];foreach(var r in store.Data["Factures"]){string d,t;if(r.TryGetValue("Date",out d)&&r.TryGetValue("Total",out t)){DateTime dt;decimal v;if(DateTime.TryParse(d,out dt)&&decimal.TryParse(t,NumberStyles.Any,CultureInfo.InvariantCulture,out v)&&dt.Year==DateTime.Now.Year)a[dt.Month-1]+=v;}}return a;}
  Control BuildModulePage(string name){
   if(name=="Paramètres")return BuildSettings();
   var p=new Panel{BackColor=canvas,Padding=new Padding(12)};
   var heading=new Label{Text=name,Font=new Font("Segoe UI",20,FontStyle.Bold),ForeColor=ink,AutoSize=true,Location=new Point(18,12)};p.Controls.Add(heading);
   var actions=new FlowLayoutPanel{Location=new Point(18,58),Size=new Size(1000,45),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,FlowDirection=FlowDirection.LeftToRight};p.Controls.Add(actions);
   var grid=new DataGridView{Location=new Point(18,112),Size=new Size(1050,470),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,RowHeadersVisible=false};p.Controls.Add(grid);
   var add=MakeButton("+ Nouveau",125,36);var edit=MakeButton("Modifier",110,36);var del=MakeButton("Supprimer",110,36);var refresh=MakeButton("Actualiser",110,36);
   actions.Controls.Add(add);actions.Controls.Add(edit);actions.Controls.Add(del);actions.Controls.Add(refresh);
   Action reload=delegate{BindGrid(name,grid);};reload();
   add.Click+=delegate{EditRecord(name,null,reload);};edit.Click+=delegate{if(grid.CurrentRow==null)return;EditRecord(name,grid.CurrentRow.Tag as Dictionary<string,string>,reload);};
   del.Click+=delegate{if(grid.CurrentRow==null)return;if(MessageBox.Show("Supprimer cet enregistrement ?","Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){var rec=grid.CurrentRow.Tag as Dictionary<string,string>;if(rec!=null)store.Data[name].Remove(rec);SaveStore();reload();}};
   refresh.Click+=delegate{reload();};
   var footer=new Label{Text="Double-cliquez sur une ligne pour la modifier. Les données restent stockées sur ce PC.",ForeColor=Color.Gray,AutoSize=true,Location=new Point(20,592)};p.Controls.Add(footer);
   grid.CellDoubleClick+=delegate{if(grid.CurrentRow!=null)EditRecord(name,grid.CurrentRow.Tag as Dictionary<string,string>,reload);};
   return p;
  }
  void BindGrid(string name,DataGridView grid){
   grid.Columns.Clear();grid.Rows.Clear();var fields=modules[name].Fields;
   foreach(string f in fields)grid.Columns.Add(f,f);
   foreach(var rec in store.Data[name]){int i=grid.Rows.Add(fields.Select(f=>rec.ContainsKey(f)?rec[f]:"").ToArray());grid.Rows[i].Tag=rec;}
  }
  void EditRecord(string name,Dictionary<string,string> existing,Action done){
   var fields=modules[name].Fields;var form=new Form{Text=(existing==null?"Nouvel enregistrement - ":"Modifier - ")+name,StartPosition=FormStartPosition.CenterParent,Size=new Size(560,Math.Min(760,160+fields.Length*48)),MinimumSize=new Size(500,400),BackColor=canvas,Font=Font};
   var panel=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(18)};form.Controls.Add(panel);
   var inputs=new Dictionary<string,Control>();int y=12;
   foreach(string field in fields){panel.Controls.Add(new Label{Text=field,Location=new Point(12,y+5),Width=175,ForeColor=ink});Control c;
    if(field=="Statut"||field=="Périodicité"||field=="Type"||field=="Mode"||field=="Actif"){var cb=new ComboBox{DropDownStyle=ComboBoxStyle.DropDown,Location=new Point(195,y),Width=300};c=cb;if(field=="Statut"){cb.Items.AddRange(new object[]{"En attente","Payé","Partiel","En cours","Terminé","Annulé"});}else if(field=="Périodicité")cb.Items.AddRange(new object[]{"Mensuel","Trimestriel","Annuel","Ponctuel"});else if(field=="Actif")cb.Items.AddRange(new object[]{"Oui","Non"});else if(field=="Mode")cb.Items.AddRange(new object[]{"Espèces","Carte","Virement","Chèque"});else cb.Items.AddRange(new object[]{"Recette","Dépense","Achat","Vente","Loyer","Autre"});}
    else {var tb=new TextBox{Location=new Point(195,y),Width=300};c=tb;}
    string val="";if(existing!=null)existing.TryGetValue(field,out val);c.Text=val??"";panel.Controls.Add(c);inputs[field]=c;y+=42;
   }
   var save=MakeButton("Enregistrer",130,38);save.Location=new Point(365,y+12);panel.Controls.Add(save);
   save.Click+=delegate{
    var rec=existing??new Dictionary<string,string>();foreach(string f in fields)rec[f]=inputs[f].Text.Trim();
    if(name=="Heures de travail")CalculateHours(rec);
    if((name=="Devis"||name=="Factures")&&String.IsNullOrWhiteSpace(rec.ContainsKey("Total")?rec["Total"]:""))rec["Total"]=CalculateInvoice(rec).ToString(CultureInfo.InvariantCulture);
    if(existing==null)store.Data[name].Add(rec);SaveStore();done();form.Close();
   };
   form.ShowDialog(this);
  }
  void CalculateHours(Dictionary<string,string> r){
   DateTime a,b;decimal pause=0,rate=0;decimal.TryParse(Get(r,"Pause (h)"),NumberStyles.Any,CultureInfo.InvariantCulture,out pause);decimal.TryParse(Get(r,"Taux horaire"),NumberStyles.Any,CultureInfo.InvariantCulture,out rate);
   if(DateTime.TryParse(Get(r,"Heure début"),out a)&&DateTime.TryParse(Get(r,"Heure fin"),out b)){decimal h=(decimal)(b-a).TotalHours-pause;if(h<0)h+=24;r["Heures nettes"]=Math.Max(0,h).ToString("0.00",CultureInfo.InvariantCulture);r["Coût main-d'œuvre"]=(Math.Max(0,h)*rate).ToString("0.00",CultureInfo.InvariantCulture);}
  }
  decimal CalculateInvoice(Dictionary<string,string> r){decimal h=Num(r,"Heures MO"),rate=Num(r,"Tarif horaire"),parts=Num(r,"Pièces HT"),discount=Num(r,"Remise"),vat=Num(r,"TVA %");decimal net=Math.Max(0,h*rate+parts-discount);return net*(1+vat/100);}
  decimal Num(Dictionary<string,string> r,string k){decimal n;decimal.TryParse(Get(r,k).Replace(',','.'),NumberStyles.Any,CultureInfo.InvariantCulture,out n);return n;}
  string Get(Dictionary<string,string> r,string k){string v;return r.TryGetValue(k,out v)?v:"";}
  Control BuildSettings(){
   var p=new Panel{BackColor=canvas,Padding=new Padding(22),AutoScroll=true};
   p.Controls.Add(new Label{Text="Paramètres de l'entreprise",Font=new Font("Segoe UI",21,FontStyle.Bold),ForeColor=ink,AutoSize=true,Location=new Point(22,18)});
   string[] labels={"Nom de l'entreprise","Devise principale","Devises disponibles","Adresse","Téléphone","E-mail","Matricule fiscal","Taux TVA par défaut"};
   var fields=new Dictionary<string,TextBox>();int y=82;
   foreach(string label in labels){p.Controls.Add(new Label{Text=label,Location=new Point(28,y+5),Width=200,ForeColor=ink});var t=new TextBox{Location=new Point(235,y),Width=430,Height=28};string value=SettingValue(label);t.Text=value;fields[label]=t;p.Controls.Add(t);y+=48;}
   var logo=MakeButton("Choisir le logo",160,38);logo.Location=new Point(235,y+10);p.Controls.Add(logo);logo.Click+=delegate{using(var d=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.bmp"})if(d.ShowDialog(this)==DialogResult.OK){try{string dest=Path.Combine(root,"company-logo.png");using(var im=Image.FromFile(d.FileName))using(var bm=new Bitmap(im))bm.Save(dest,System.Drawing.Imaging.ImageFormat.Png);store.Logo=dest;MessageBox.Show("Logo enregistré.");}catch(Exception ex){MessageBox.Show(ex.Message);}}};
   var save=MakeButton("Enregistrer les paramètres",230,42);save.Location=new Point(410,y+10);p.Controls.Add(save);save.Click+=delegate{
    store.Company=fields["Nom de l'entreprise"].Text.Trim();if(store.Company=="")store.Company="Mon Garage";store.Currency=fields["Devise principale"].Text.Trim();if(store.Currency=="")store.Currency="TND";store.Currencies=fields["Devises disponibles"].Text;SaveStore();companyLabel.Text=store.Company;MessageBox.Show("Paramètres enregistrés.","Garage Gestion",MessageBoxButtons.OK,MessageBoxIcon.Information);
   };
   var info=new Label{Text="Astuce : ajoutez une ligne dans « Loyer et charges » pour chaque échéance de loyer. Les heures de travail et le taux horaire des employés alimentent le calcul du coût de main-d'œuvre.",Location=new Point(28,y+75),Size=new Size(760,60),ForeColor=Color.Gray};p.Controls.Add(info);
   return p;
  }
  string SettingValue(string label){switch(label){case "Nom de l'entreprise":return store.Company;case "Devise principale":return store.Currency;case "Devises disponibles":return store.Currencies;default:return "";}}
  sealed class ChartPanel:Panel{
   public decimal[] Values=new decimal[12];public string Title="";
   protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var f=new Font("Segoe UI",12,FontStyle.Bold))using(var brush=new SolidBrush(Color.FromArgb(39,53,72)))g.DrawString(Title,f,brush,18,15);
    int left=48,top=65,w=Math.Max(100,Width-78),h=Math.Max(100,Height-112);using(var pen=new Pen(Color.FromArgb(230,235,241)))for(int i=0;i<5;i++){int yy=top+i*h/4;g.DrawLine(pen,left,yy,left+w,yy);}
    decimal max=Values.Max();if(max<=0)max=1;PointF[] pts=new PointF[12];for(int i=0;i<12;i++){float x=left+i*w/11f;float y=top+h-(float)(Values[i]/max)*(h-8);pts[i]=new PointF(x,y);}
    using(var pen=new Pen(Color.FromArgb(42,112,190),3))g.DrawLines(pen,pts);using(var b=new SolidBrush(Color.FromArgb(42,112,190)))foreach(var pt in pts)g.FillEllipse(b,pt.X-3,pt.Y-3);
    string[] months={"J","F","M","A","M","J","J","A","S","O","N","D"};using(var f=new Font("Segoe UI",8))using(var b=new SolidBrush(Color.Gray))for(int i=0;i<12;i++)g.DrawString(months[i],f,b,left+i*w/11f-4,top+h+12);
   }
  }
 }
}