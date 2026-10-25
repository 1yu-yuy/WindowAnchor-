using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowAnchor {
 static class Style {
  public static readonly Color Ink=Color.FromArgb(30,38,51), Muted=Color.FromArgb(120,130,145), Accent=Color.FromArgb(66,91,220), Line=Color.FromArgb(230,234,240);
  public static GraphicsPath Round(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 }
 sealed class Card:Panel {
  public Card(){DoubleBuffered=true;BackColor=Color.Transparent;}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Style.Round(new Rectangle(0,0,Width-1,Height-1),14))using(var b=new SolidBrush(Color.White))using(var pen=new Pen(Style.Line)){e.Graphics.FillPath(b,p);e.Graphics.DrawPath(pen,p);}base.OnPaint(e);}
 }
 sealed class ActionButton:Button {
  public bool Primary; bool hover;
  public ActionButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var color=Primary?(hover?Color.FromArgb(53,75,196):Style.Accent):(hover?Color.FromArgb(235,239,248):Color.FromArgb(245,247,251));using(var p=Style.Round(new Rectangle(0,0,Width-1,Height-1),8))using(var b=new SolidBrush(color))e.Graphics.FillPath(b,p);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?(Primary?Color.White:Style.Ink):Style.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-8,Height-8));}
 }
 sealed class MapPreview:Control {
  public Rectangle Area,Target;
  public MapPreview(){DoubleBuffered=true;BackColor=Color.FromArgb(247,249,252);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var canvas=new Rectangle(18,22,Width-36,Height-70);using(var p=Style.Round(canvas,7))using(var b=new SolidBrush(Color.FromArgb(231,236,245)))g.FillPath(b,p);if(Area.Width>0&&Area.Height>0){float scale=Math.Min((float)(canvas.Width-18)/Area.Width,(float)(canvas.Height-18)/Area.Height);var desk=new RectangleF(canvas.Left+(canvas.Width-Area.Width*scale)/2,canvas.Top+(canvas.Height-Area.Height*scale)/2,Area.Width*scale,Area.Height*scale);using(var p=new Pen(Color.FromArgb(197,206,221)))g.DrawRectangle(p,desk.X,desk.Y,desk.Width,desk.Height);var box=new RectangleF(desk.X+(Target.X-Area.X)*scale,desk.Y+(Target.Y-Area.Y)*scale,Math.Max(3,Target.Width*scale),Math.Max(3,Target.Height*scale));g.SetClip(canvas);using(var b=new SolidBrush(Color.FromArgb(55,66,91,220)))g.FillRectangle(b,box);using(var p=new Pen(Style.Accent,2))g.DrawRectangle(p,box.X,box.Y,box.Width,box.Height);g.ResetClip();}TextRenderer.DrawText(g,"位置预览",Font,new Rectangle(0,Height-34,Width,25),Style.Muted,TextFormatFlags.HorizontalCenter);}
 }
 sealed class RegionSelector:Form {
  readonly Rectangle desktop; Point start,current; bool dragging; public Rectangle Selection {get;private set;}
  public RegionSelector(){Icon=Program.LoadIcon();Text="选择窗口区域 · Esc 取消";desktop=SystemInformation.VirtualScreen;AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=desktop;TopMost=true;ShowInTaskbar=true;DoubleBuffered=true;KeyPreview=true;Cursor=Cursors.Cross;BackColor=Color.FromArgb(21,27,39);Font=new Font("Microsoft YaHei UI",11);}
  public static Rectangle FromPoints(Point a,Point b){return Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Right){DialogResult=DialogResult.Cancel;Close();return;}if(e.Button!=MouseButtons.Left)return;start=current=e.Location;dragging=true;Capture=true;Invalidate();}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);current=e.Location;Invalidate();}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(!dragging||e.Button!=MouseButtons.Left)return;current=e.Location;var r=FromPoints(start,current);Capture=false;dragging=false;if(r.Width<40||r.Height<40){Invalidate();return;}r.Offset(desktop.Location);Selection=r;DialogResult=DialogResult.OK;Close();}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var grid=new Pen(Color.FromArgb(30,255,255,255))){for(int i=0;i<Width;i+=80)g.DrawLine(grid,i,0,i,Height);for(int j=0;j<Height;j+=80)g.DrawLine(grid,0,j,Width,j);}
   foreach(var s in Screen.AllScreens){var r=s.Bounds;r.Offset(-desktop.Left,-desktop.Top);using(var p=new Pen(Color.FromArgb(65,255,255,255)))g.DrawRectangle(p,r.X+1,r.Y+1,r.Width-3,r.Height-3);TextRenderer.DrawText(g,"屏幕原点  X "+s.Bounds.X+"   Y "+s.Bounds.Y+"     "+s.Bounds.Width+" × "+s.Bounds.Height,Font,new Point(r.Left+24,r.Top+22),Color.FromArgb(169,183,207));}
   var primary=Screen.PrimaryScreen.Bounds;primary.Offset(-desktop.Left,-desktop.Top);var title=new Rectangle(primary.Left+50,primary.Top+85,primary.Width-100,48);using(var big=new Font(Font.FontFamily,24,FontStyle.Bold))TextRenderer.DrawText(g,"拖动，选出窗口的位置和大小",big,title,Color.White,TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(g,"松开鼠标完成  ·  Esc / 右键取消  ·  最小区域 40 × 40",Font,new Rectangle(title.X,title.Bottom+10,title.Width,30),Color.FromArgb(165,179,204),TextFormatFlags.HorizontalCenter);
   var selected=FromPoints(start,current);if(dragging){using(var b=new SolidBrush(Color.FromArgb(65,105,135,255)))g.FillRectangle(b,selected);using(var p=new Pen(Color.FromArgb(139,166,255),2))g.DrawRectangle(p,selected);}
   using(var p=new Pen(Color.FromArgb(90,170,190,230))){p.DashStyle=DashStyle.Dash;g.DrawLine(p,current.X,0,current.X,Height);g.DrawLine(p,0,current.Y,Width,current.Y);}
   string data=dragging?"X "+(selected.X+desktop.X)+"   Y "+(selected.Y+desktop.Y)+"   W "+selected.Width+"   H "+selected.Height:"X "+(current.X+desktop.X)+"   Y "+(current.Y+desktop.Y);
   int labelWidth=dragging?420:235;int lx=Math.Max(8,Math.Min(Width-labelWidth-8,current.X+20)),ly=Math.Max(8,Math.Min(Height-52,current.Y+24));using(var p=Style.Round(new Rectangle(lx,ly,labelWidth,40),7))using(var b=new SolidBrush(Color.FromArgb(49,62,85)))g.FillPath(b,p);TextRenderer.DrawText(g,data,Font,new Rectangle(lx+10,ly,labelWidth-20,40),Color.White,TextFormatFlags.VerticalCenter);
  }
 }
}
