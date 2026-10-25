using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowAnchor {
 static class Native {
  public delegate bool EnumProc(IntPtr h, IntPtr p);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; public Rectangle Rectangle { get { return Rectangle.FromLTRB(Left,Top,Right,Bottom); } } }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
  [StructLayout(LayoutKind.Sequential)] public struct Placement { public int length,flags,showCmd; public POINT min,max; public RECT normal; }
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr p);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsHungAppWindow(IntPtr h);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,StringBuilder b,int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int index);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT rect);
  [DllImport("user32.dll")] public static extern bool GetWindowPlacement(IntPtr h,ref Placement placement);
  [DllImport("user32.dll")] public static extern bool SetWindowPlacement(IntPtr h,ref Placement placement);
  [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h,int cmd);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public static string Title(IntPtr h) { var b=new StringBuilder(1024); GetWindowText(h,b,b.Capacity); return b.ToString(); }
  public static bool Topmost(IntPtr h) { return (GetWindowLong(h,-20)&8)!=0; }
 }
 sealed class WindowItem { public IntPtr Handle; public uint Pid; public string Name; public override string ToString(){return Name;} }
 sealed class Anchor {
  public WindowItem Window; public Native.Placement Original; public bool OriginalTopmost, Lock, Top; public Rectangle Target;
 }
 static class Program {
  public static Icon LoadIcon(){using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowAnchor.App.ico")){if(stream==null)return (Icon)SystemIcons.Application.Clone();using(var icon=new Icon(stream,32,32))return (Icon)icon.Clone();}}
  [STAThread] static void Main(string[] args) {
   Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   if(args.Length>0 && args[0]=="--selftest") { try { SelfTest(args[1]); } catch(Exception ex) { File.WriteAllText(args[1],ex.ToString()); Environment.ExitCode=1; } return; }
   Application.Run(new MainForm());
  }
  public static Rectangle Position(Rectangle area, Size size, int preset, int margin, int x, int y) {
   int w=Math.Min(size.Width,area.Width),h=Math.Min(size.Height,area.Height);
   int mx=Math.Min(margin,Math.Max(0,(area.Width-w)/2)), my=Math.Min(margin,Math.Max(0,(area.Height-h)/2));
   int left=area.Left+mx, right=area.Right-w-mx, top=area.Top+my, bottom=area.Bottom-h-my;
   switch(preset) {
    case 0: return new Rectangle(right,top,w,h);
    case 1: return new Rectangle(area.Left+(area.Width-w)/2,top,w,h);
    case 2: return new Rectangle(left,top,w,h);
    case 3: return new Rectangle(right,bottom,w,h);
    case 4: return new Rectangle(left,bottom,w,h);
    case 5: return new Rectangle(area.Left+(area.Width-w)/2,area.Top+(area.Height-h)/2,w,h);
    case 6: return new Rectangle(x,y,w,h);
    default: return new Rectangle(x,y,size.Width,size.Height);
   }
  }
  static void Check(bool pass,string message) { if(!pass) throw new Exception(message); }
  static void SelfTest(string path) {
   var results=new List<string>();using(var embedded=Program.LoadIcon()){Check(embedded.Width==32&&System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowAnchor.App.ico")!=null,"图标资源载入失败");results.Add("PASS: embedded application icon loads at 32px");}
   Check(Position(new Rectangle(-1920,0,1920,1040),new Size(500,300),0,12,0,0)==new Rectangle(-512,12,500,300),"多显示器定位失败"); results.Add("PASS: negative-coordinate monitor placement");
   Check(Position(new Rectangle(0,0,800,600),new Size(1000,700),1,12,0,0)==new Rectangle(0,0,800,600),"超大窗口适配失败"); results.Add("PASS: oversized window fit");
   using(var target=new Form()) using(var app=new MainForm()) {
    target.Text="WindowAnchor test target"; target.StartPosition=FormStartPosition.Manual; target.Bounds=new Rectangle(100,120,420,280); target.Show(); Application.DoEvents();
    var initial=target.Bounds; var item=new WindowItem {Handle=target.Handle,Pid=(uint)System.Diagnostics.Process.GetCurrentProcess().Id,Name=target.Text};
    app.Apply(item,new Rectangle(220,180,420,280),true,true); Application.DoEvents();
    Check(target.Bounds.Location==new Point(220,180),"窗口移动失败"); Check(Native.Topmost(target.Handle),"置顶失败"); results.Add("PASS: actual HWND move and topmost");
    target.Location=new Point(330,330); app.Enforce(); Application.DoEvents(); Check(target.Location==new Point(220,180),"位置锁定失败"); results.Add("PASS: position lock correction");
    app.Release(target.Handle); Application.DoEvents(); Check(target.Bounds==initial,"原位置恢复失败"); Check(!Native.Topmost(target.Handle),"置顶恢复失败"); results.Add("PASS: restore original bounds and z-order state");
    target.TopMost=true; app.Apply(item,new Rectangle(240,200,420,280),false,false); app.Release(target.Handle); Check(Native.Topmost(target.Handle),"原始置顶状态恢复失败"); target.TopMost=false; results.Add("PASS: preserve pre-existing topmost state");
    target.WindowState=FormWindowState.Maximized; Application.DoEvents(); var task=app.ApplyWindow(item); var deadline=DateTime.UtcNow.AddSeconds(5); while(!task.IsCompleted&&DateTime.UtcNow<deadline){Application.DoEvents();System.Threading.Thread.Sleep(10);} Check(task.IsCompleted&&!task.IsFaulted,"最大化窗口异步应用失败"); Check(!Native.IsZoomed(target.Handle),"最大化窗口未还原"); Check(app.AnchorCount==1,"最大化窗口未管理"); app.Release(target.Handle);Application.DoEvents();Check(Native.IsZoomed(target.Handle),"原始最大化状态恢复失败");target.WindowState=FormWindowState.Normal;Application.DoEvents();results.Add("PASS: apply to maximized window and restore maximized state");
    var region=new Rectangle(160,190,510,340);app.SetCustomRegion(region);var regionTask=app.ApplyWindow(item);while(!regionTask.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}Check(target.Bounds==region,"框选坐标与尺寸应用失败");app.Release(target.Handle);results.Add("PASS: custom region applies X, Y, width and height");
    app.Apply(item,target.Bounds,true,false); target.Close(); app.Enforce(); Check(app.AnchorCount==0,"关闭窗口清理失败"); results.Add("PASS: closed window cleanup");
    app.Show(); Application.DoEvents(); using(var bmp=new Bitmap(app.Width,app.Height)) { app.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size)); bmp.Save(Path.Combine(Path.GetDirectoryName(path),"preview.png")); } app.Close();
   }
   Check(RegionSelector.FromPoints(new Point(500,400),new Point(120,160))==new Rectangle(120,160,380,240),"反向框选失败");results.Add("PASS: reverse-direction region normalization");
   using(var selector=new RegionSelector()){
    selector.Show();Application.DoEvents();using(var bmp=new Bitmap(selector.Width,selector.Height)){selector.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(Path.GetDirectoryName(path),"selector-preview.png"));}
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    typeof(RegionSelector).GetMethod("OnMouseDown",flags).Invoke(selector,new object[]{new MouseEventArgs(MouseButtons.Left,1,450,420,0)});
    typeof(RegionSelector).GetMethod("OnMouseMove",flags).Invoke(selector,new object[]{new MouseEventArgs(MouseButtons.Left,0,100,140,0)});
    using(var bmp=new Bitmap(selector.Width,selector.Height)){selector.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(Path.GetDirectoryName(path),"selector-drag.png"));}
    typeof(RegionSelector).GetMethod("OnMouseUp",flags).Invoke(selector,new object[]{new MouseEventArgs(MouseButtons.Left,1,100,140,0)});
    var desktop=SystemInformation.VirtualScreen;Check(selector.Selection==new Rectangle(desktop.X+100,desktop.Y+140,350,280),"选区桌面坐标转换失败");Check(selector.DialogResult==DialogResult.OK,"选区确认失败");results.Add("PASS: overlay drag converts to absolute desktop rectangle");
   }
   using(var selector=new RegionSelector()) {selector.Show();Application.DoEvents();typeof(RegionSelector).GetMethod("OnKeyDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(selector,new object[]{new KeyEventArgs(Keys.Escape)});Check(selector.DialogResult==DialogResult.Cancel,"Esc 取消失败");results.Add("PASS: Escape cancels overlay");}
   File.WriteAllLines(path,results.ToArray());
  }
 }
 sealed class MainForm:Form {
  readonly ComboBox windows=new ComboBox(), displays=new ComboBox(), presets=new ComboBox();
  readonly NumericUpDown x=new NumericUpDown(),y=new NumericUpDown(),wide=new NumericUpDown(),high=new NumericUpDown(),margin=new NumericUpDown();
  readonly CheckBox lockBox=new CheckBox(),topBox=new CheckBox(); readonly ListView active=new ListView(); readonly Label status=new Label(),currentInfo=new Label(),screenInfo=new Label(),countInfo=new Label();
  readonly MapPreview preview=new MapPreview();
  readonly Dictionary<IntPtr,Anchor> anchors=new Dictionary<IntPtr,Anchor>(); readonly Timer timer=new Timer(), picker=new Timer(); readonly NotifyIcon tray=new NotifyIcon();
  int countdown,lastPreset; bool updating,selecting; Button pickButton,regionButton; public int AnchorCount {get{return anchors.Count;}}
  public MainForm() {
   AutoScaleMode=AutoScaleMode.None;Icon=Program.LoadIcon();Text="窗口固定助手 · 2.2";Font=new Font("Microsoft YaHei UI",9.5f);ForeColor=Style.Ink;ClientSize=new Size(860,790);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(245,247,251);DoubleBuffered=true;
   LabelAt("窗口固定助手",24,20,560,30,22,true,Style.Ink);
   LabelAt("让每个窗口，都有自己的位置。",26,61,570,24,10,false,Style.Muted);
   ButtonAt("收起到托盘",704,33,132,36,false,delegate{Hide();});

   LabelAt("目标窗口",44,113,200,22,10,true,Style.Ink);
   ConfigureCombo(windows,44,140,490);ButtonAt("刷新",546,138,76,34,false,delegate{RefreshWindows();});
   pickButton=ButtonAt("点选窗口 · 3s",634,138,182,34,false,delegate {countdown=3;pickButton.Enabled=false;picker.Start();status.Text="请在 3 秒内点击目标窗口";});
   currentInfo.SetBounds(44,174,756,20);currentInfo.ForeColor=Style.Muted;currentInfo.Font=new Font(Font.FontFamily,8.5f);currentInfo.BackColor=Color.White;Controls.Add(currentInfo);
   LabelAt("位置与区域",44,232,220,25,11,true,Style.Ink);
   LabelAt("预设位置（可选）",44,342,220,20,9,false,Style.Muted);LabelAt("显示器",280,342,245,20,9,false,Style.Muted);
   ConfigureCombo(presets,44,366,220);presets.Items.AddRange(new object[]{"右上角","顶部居中","左上角","右下角","左下角","屏幕居中","自定义区域 · 框选","保持当前位置"});presets.SelectedIndex=0;
   ConfigureCombo(displays,280,366,246);LoadDisplays();
   string[] labels={"X · 横坐标","Y · 纵坐标","W · 宽度","H · 高度"};NumericUpDown[] numbers={x,y,wide,high};for(int i=0;i<4;i++){LabelAt(labels[i],44+i*122,411,115,20,8.5f,false,Style.Muted);SetupNumber(numbers[i],44+i*122,435,i<2?-100000:40,i<2?100000:100000);numbers[i].ValueChanged+=delegate{if(!updating)UpdatePreview();};}
   lockBox.Text="锁定位置";lockBox.Checked=true;lockBox.SetBounds(44,482,140,28);lockBox.BackColor=Color.White;Controls.Add(lockBox);topBox.Text="始终置顶";topBox.Checked=true;topBox.SetBounds(205,482,140,28);topBox.BackColor=Color.White;Controls.Add(topBox);
   regionButton=ButtonAt("手动框选区域",44,270,482,58,true,delegate{if(presets.SelectedIndex!=6)presets.SelectedIndex=6;else SelectRegion();});regionButton.Font=new Font(Font.FontFamily,15,FontStyle.Bold);
   preview.SetBounds(550,270,266,210);Controls.Add(preview);screenInfo.SetBounds(550,489,266,22);screenInfo.Font=new Font(Font.FontFamily,8);screenInfo.ForeColor=Style.Muted;screenInfo.BackColor=Color.White;Controls.Add(screenInfo);
   LabelAt("边距",550,543,42,24,9,false,Style.Muted);SetupNumber(margin,594,539,0,500);margin.Width=70;margin.Value=12;LabelAt("px",672,543,35,24,9,false,Style.Muted);margin.ValueChanged+=delegate{if(!updating)UpdatePreview();};
   ButtonAt("应用到窗口",44,532,300,40,false,delegate{ApplySelected();});LabelAt("解除或退出时，恢复原位",362,543,184,24,8.5f,false,Style.Muted);
   countInfo.SetBounds(44,620,260,23);countInfo.Text="已固定 · 0";countInfo.Font=new Font(Font.FontFamily,10,FontStyle.Bold);countInfo.BackColor=Color.White;Controls.Add(countInfo);
   ButtonAt("解除选中",598,615,100,30,false,delegate{if(active.SelectedItems.Count>0)Release((IntPtr)active.SelectedItems[0].Tag);});ButtonAt("全部恢复",708,615,108,30,false,delegate{ReleaseAll();});
   active.SetBounds(44,654,772,69);active.View=View.Details;active.BorderStyle=BorderStyle.None;active.FullRowSelect=true;active.HideSelection=false;active.MultiSelect=false;active.HeaderStyle=ColumnHeaderStyle.Nonclickable;active.Columns.Add("窗口",255);active.Columns.Add("X / Y",145);active.Columns.Add("W × H",145);active.Columns.Add("状态",200);Controls.Add(active);
   status.SetBounds(26,756,810,22);status.Text="就绪 · 选择窗口后即可应用";status.Font=new Font(Font.FontFamily,8.5f);status.ForeColor=Style.Muted;Controls.Add(status);
   windows.SelectedIndexChanged+=delegate{UpdatePreview();};displays.SelectedIndexChanged+=delegate{UpdatePreview();};
   presets.SelectedIndexChanged+=delegate{if(updating||selecting)return;if(presets.SelectedIndex==6){BeginInvoke(new Action(SelectRegion));}else{lastPreset=presets.SelectedIndex;UpdatePreview();}};
   timer.Interval=250;timer.Tick+=delegate{if(!selecting){Enforce();UpdateCurrentInfo();}};timer.Start();
   picker.Interval=1000;picker.Tick+=delegate{countdown--;pickButton.Text="点选窗口 · "+countdown+"s";if(countdown>0)return;picker.Stop();pickButton.Text="点选窗口 · 3s";pickButton.Enabled=true;var h=Native.GetForegroundWindow();RefreshWindows();for(int i=0;i<windows.Items.Count;i++)if(((WindowItem)windows.Items[i]).Handle==h){windows.SelectedIndex=i;status.Text="已选择目标窗口";Show();Activate();return;}status.Text="未选到普通窗口，请重试或从列表选择";};
   tray.Icon=Icon;tray.Text="窗口固定助手";tray.Visible=true;tray.DoubleClick+=delegate{Show();WindowState=FormWindowState.Normal;Activate();};var menu=new ContextMenuStrip();menu.Items.Add("打开助手",null,delegate{Show();WindowState=FormWindowState.Normal;Activate();});menu.Items.Add("全部恢复",null,delegate{ReleaseAll();});menu.Items.Add("退出并恢复窗口",null,delegate{Close();});tray.ContextMenuStrip=menu;
   FormClosing+=delegate{timer.Stop();picker.Stop();ReleaseAll();tray.Visible=false;tray.Dispose();timer.Dispose();picker.Dispose();};RefreshWindows();UpdatePreview();
  }
  protected override void OnPaintBackground(PaintEventArgs e){
   base.OnPaintBackground(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   foreach(var rect in new[]{new Rectangle(24,100,812,100),new Rectangle(24,216,812,374),new Rectangle(24,606,812,132)}){
    using(var shape=Style.Round(rect,14))using(var fill=new SolidBrush(Color.White))using(var border=new Pen(Style.Line)){e.Graphics.FillPath(fill,shape);e.Graphics.DrawPath(border,shape);}
   }
  }
  void LabelAt(string text,int left,int top,int width,int height,float size,bool bold,Color color){var label=new Label{Text=text,Location=new Point(left,top),Size=new Size(width,height),Font=new Font(Font.FontFamily,size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=top>=100&&top<738?Color.White:BackColor};Controls.Add(label);}
  Button ButtonAt(string text,int left,int top,int width,int height,bool primary,EventHandler action){var b=new ActionButton{Text=text,Primary=primary,BackColor=top>=100&&top<738?Color.White:BackColor,Location=new Point(left,top),Size=new Size(width,height)};b.Click+=action;Controls.Add(b);return b;}
  void ConfigureCombo(ComboBox box,int left,int top,int width){box.SetBounds(left,top,width,32);box.DropDownStyle=ComboBoxStyle.DropDownList;box.FlatStyle=FlatStyle.Flat;box.BackColor=Color.FromArgb(246,248,252);box.ForeColor=Style.Ink;box.IntegralHeight=false;box.DropDownHeight=260;Controls.Add(box);}
  void SetupNumber(NumericUpDown n,int left,int top,int min,int max){n.SetBounds(left,top,108,29);n.Minimum=min;n.Maximum=max;n.BorderStyle=BorderStyle.FixedSingle;n.Font=new Font("Consolas",11);n.BackColor=Color.FromArgb(249,250,253);n.ForeColor=Style.Ink;Controls.Add(n);}
  void LoadDisplays(){displays.Items.Add("跟随目标窗口");int i=1;foreach(var s in Screen.AllScreens)displays.Items.Add("屏幕 "+i+++" · "+s.Bounds.Width+" × "+s.Bounds.Height+(s.Primary?" · 主屏":""));displays.SelectedIndex=0;}
  Screen SelectedScreen(){var screens=Screen.AllScreens;if(displays.SelectedIndex>0&&displays.SelectedIndex<=screens.Length)return screens[displays.SelectedIndex-1];var w=windows.SelectedItem as WindowItem;return w!=null&&Valid(w)?Screen.FromHandle(w.Handle):Screen.PrimaryScreen;}
  void UpdateCurrentInfo(){var w=windows.SelectedItem as WindowItem;Native.RECT r;if(w!=null&&Valid(w)&&Native.GetWindowRect(w.Handle,out r))currentInfo.Text="当前窗口    X "+r.Left+"    Y "+r.Top+"    W "+(r.Right-r.Left)+"    H "+(r.Bottom-r.Top)+"  px";else currentInfo.Text="选择一个窗口，查看实时坐标和尺寸";}
  void UpdatePreview(){if(updating)return;var area=SelectedScreen().WorkingArea;int preset=presets.SelectedIndex;var w=windows.SelectedItem as WindowItem;Native.RECT r;Rectangle bounds=w!=null&&Valid(w)&&Native.GetWindowRect(w.Handle,out r)?r.Rectangle:new Rectangle(area.X+100,area.Y+100,Math.Min(800,area.Width),Math.Min(560,area.Height));Rectangle target=preset==6?new Rectangle((int)x.Value,(int)y.Value,(int)wide.Value,(int)high.Value):Program.Position(area,bounds.Size,preset,(int)margin.Value,bounds.X,bounds.Y);updating=true;if(preset!=6){SetNumber(x,target.X);SetNumber(y,target.Y);SetNumber(wide,target.Width);SetNumber(high,target.Height);}foreach(var n in new[]{x,y,wide,high}){n.ReadOnly=preset!=6;n.InterceptArrowKeys=preset==6;n.Enabled=preset==6;}margin.Enabled=preset!=6&&preset!=7;regionButton.Enabled=true;updating=false;preview.Area=area;preview.Target=target;preview.Invalidate();screenInfo.Text="屏幕原点  "+area.X+", "+area.Y+"   ·   单位 px";UpdateCurrentInfo();}
  void SetNumber(NumericUpDown n,int value){n.Value=Math.Max(n.Minimum,Math.Min(n.Maximum,value));}
  public void SetCustomRegion(Rectangle region){updating=true;presets.SelectedIndex=6;SetNumber(x,region.X);SetNumber(y,region.Y);SetNumber(wide,region.Width);SetNumber(high,region.Height);updating=false;lastPreset=6;regionButton.Text="重新框选区域";UpdatePreview();}
  void SelectRegion(){if(selecting||IsDisposed)return;selecting=true;picker.Stop();pickButton.Enabled=true;pickButton.Text="点选窗口 · 3s";Hide();try{using(var overlay=new RegionSelector()){if(overlay.ShowDialog()==DialogResult.OK){SetCustomRegion(overlay.Selection);status.Text="区域已选定 · 点击应用，将窗口放入选区";}else{updating=true;presets.SelectedIndex=lastPreset;updating=false;UpdatePreview();status.Text="已取消框选";}}}finally{selecting=false;Show();Activate();}}

  void RefreshWindows(){IntPtr selected=windows.SelectedItem==null?IntPtr.Zero:((WindowItem)windows.SelectedItem).Handle;windows.Items.Clear();uint mine=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;Native.EnumWindows(delegate(IntPtr h,IntPtr p){uint pid;Native.GetWindowThreadProcessId(h,out pid);string title=Native.Title(h);if(pid!=mine&&Native.IsWindowVisible(h)&&title.Length>0&&(Native.GetWindowLong(h,-20)&0x80)==0)windows.Items.Add(new WindowItem{Handle=h,Pid=pid,Name=title});return true;},IntPtr.Zero);for(int i=0;i<windows.Items.Count;i++)if(((WindowItem)windows.Items[i]).Handle==selected)windows.SelectedIndex=i;if(windows.SelectedIndex<0&&windows.Items.Count>0)windows.SelectedIndex=0;}
  bool Valid(WindowItem w){uint pid;Native.GetWindowThreadProcessId(w.Handle,out pid);return Native.IsWindow(w.Handle)&&pid==w.Pid;}
  async void ApplySelected(){await ApplyWindow(windows.SelectedItem as WindowItem);}
  public async Task ApplyWindow(WindowItem w){
   if(w==null||!Valid(w)){status.Text="窗口已关闭，请刷新后重选。";return;}
   if(Native.IsHungAppWindow(w.Handle)){status.Text="目标窗口暂时无响应，请稍后再试。";return;}
   var original=new Native.Placement();original.length=Marshal.SizeOf(typeof(Native.Placement));
   if(!Native.GetWindowPlacement(w.Handle,ref original)){status.Text="无法读取窗口位置。";return;}
   bool originalTop=Native.Topmost(w.Handle),wasManaged=anchors.ContainsKey(w.Handle);
   int preset=presets.SelectedIndex,gap=(int)margin.Value,customX=(int)x.Value,customY=(int)y.Value,customWidth=(int)wide.Value,customHeight=(int)high.Value;
   bool locked=lockBox.Checked,top=topBox.Checked;
   Screen[] screens=Screen.AllScreens; Screen screen=displays.SelectedIndex>0&&displays.SelectedIndex<=screens.Length?screens[displays.SelectedIndex-1]:Screen.FromHandle(w.Handle);
   if(Native.IsIconic(w.Handle)||Native.IsZoomed(w.Handle)){
    Native.ShowWindowAsync(w.Handle,9);
    for(int attempt=0;attempt<15;attempt++){await Task.Delay(100);if(IsDisposed||Disposing)return;if(!Valid(w)){status.Text="窗口已关闭。";return;}if(!Native.IsIconic(w.Handle)&&!Native.IsZoomed(w.Handle))break;}
    if(Native.IsIconic(w.Handle)||Native.IsZoomed(w.Handle)){status.Text="窗口无法还原，请先手动还原后重试。";return;}
   }
   Native.RECT r;if(!Native.GetWindowRect(w.Handle,out r))return;
   var rect=preset==6?new Rectangle(customX,customY,customWidth,customHeight):Program.Position(screen.WorkingArea,r.Rectangle.Size,preset,gap,preset==7?r.Left:customX,preset==7?r.Top:customY);
   try{Apply(w,rect,locked,top);if(!wasManaged){anchors[w.Handle].Original=original;anchors[w.Handle].OriginalTopmost=originalTop;}status.Text="已应用："+w.Name;}
   catch(Exception ex){status.Text=ex.Message;}
  }
  public void Apply(WindowItem w,Rectangle target,bool locked,bool top){Anchor a;bool existed=anchors.TryGetValue(w.Handle,out a);if(!existed){var placement=new Native.Placement();placement.length=Marshal.SizeOf(typeof(Native.Placement));if(!Native.GetWindowPlacement(w.Handle,ref placement))throw new Exception("无法读取窗口位置。");a=new Anchor{Window=w,Original=placement,OriginalTopmost=Native.Topmost(w.Handle)};}if(!Native.SetWindowPos(w.Handle,top?new IntPtr(-1):new IntPtr(-2),target.X,target.Y,target.Width,target.Height,0x10|0x400))throw new Exception("无法控制该窗口；若目标以管理员运行，请也以管理员身份运行助手。");a.Target=target;a.Lock=locked;a.Top=top;anchors[w.Handle]=a;RenderList();}
  public void Enforce(){var removed=new List<IntPtr>();foreach(var entry in anchors){var a=entry.Value;if(!Valid(a.Window)){removed.Add(entry.Key);continue;}if(Native.IsIconic(entry.Key)||Native.IsZoomed(entry.Key)||Native.IsHungAppWindow(entry.Key))continue;Native.RECT r;Native.GetWindowRect(entry.Key,out r);if((a.Lock&&r.Rectangle!=a.Target)||Native.Topmost(entry.Key)!=a.Top){var rect=a.Lock?a.Target:r.Rectangle;Native.SetWindowPos(entry.Key,a.Top?new IntPtr(-1):new IntPtr(-2),rect.X,rect.Y,rect.Width,rect.Height,0x10|0x400);}}foreach(var h in removed)anchors.Remove(h);if(removed.Count>0)RenderList();}
  public void Release(IntPtr h){Anchor a;if(!anchors.TryGetValue(h,out a))return;if(Valid(a.Window)){var p=a.Original;Native.SetWindowPlacement(h,ref p);Native.SetWindowPos(h,a.OriginalTopmost?new IntPtr(-1):new IntPtr(-2),0,0,0,0,0x01|0x02|0x10|0x400);}anchors.Remove(h);RenderList();status.Text="已解除并恢复原始位置。";}
  void ReleaseAll(){foreach(var h in new List<IntPtr>(anchors.Keys))Release(h);}
  void RenderList(){active.Items.Clear();foreach(var pair in anchors){var a=pair.Value;var row=new ListViewItem(a.Window.Name);row.Tag=pair.Key;row.SubItems.Add(a.Target.X+" / "+a.Target.Y);row.SubItems.Add(a.Target.Width+" × "+a.Target.Height);row.SubItems.Add((a.Lock?"位置锁定":"自由移动")+" · "+(a.Top?"置顶":"普通层级") );active.Items.Add(row);}countInfo.Text="已固定 · "+anchors.Count;}
 }
}
