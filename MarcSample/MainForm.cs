using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibraryStudio.Forms;

namespace MarcSample
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            /*
            this.marcControl1.GetFieldCaption += (field) =>
            {
                if (field.IsHeader)
                    return $"头标区";
                return $"获得 '{field.FieldName}' 的值";
            };
            */

            this.marcControl1.CaretMoved += (s, e) =>
            {
                toolStripStatusLabel_caretOffs.Text = $"Caret:{this.marcControl1.CaretOffset}";
                toolStripStatusLabel_caretFieldRegion.Text = "FieldRegion:" + this.marcControl1.CaretFieldRegion.ToString();
            };
            this.marcControl1.SelectionChanged += (s, e) =>
            {
                toolStripStatusLabel_selectionRange.Text = $"Block:{this.marcControl1.SelectionStart}-{this.marcControl1.SelectionEnd}";
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.marcControl1.ColorThemeName = "simplest";

            this.marcControl1.ClientBoundsWidth = 0;
            // this.marcControl1.ClientBoundsWidth = 800;
            // this.marcControl1.ClientBoundsWidth = -1;
            this.marcControl1.GetStructure = (path, level) =>
            {
                var root = BuildTree("unimarc");
                var result = FindPath(root, path);
                if (result == null)
                    return null;
                CutLevel(result, level);
                return result;
            };

            this.marcControl1.GetValueList = (path) =>
            {
                return FindValueList(path);
            };

            // this.marcControl1.Content = "012345678901234567890123abc12ABC\u001faAAA\u001fbBBB";
            //this.marcControl1.Content = new string((char)31, 1) + "1";
            // this.marcControl1.Content = "ش12345678901234567890123";
            // this.marcControl1.Content = "012345678901234567890123";
            this.marcControl1.Content = "012345678901234567890123abc12ABC\u001faAAA\u001fbBBB شلاؤيث ฟิแกำดเ 中文 english\u001e100  \u001fatest3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333￮﨩\u001d\u001e\u001f33333333";
            // this.marcControl1.Content = "01234567890123456789";
            this.marcControl1.PadWhileEditing = false;
            // this.marcControl1.PaddingChar = '*';

            this.marcControl1.HighlightBlankChar = '·';  // '◌'; // '▪';// '▫'; // '□'; // '⸗';

            AppUtility.LoadState(this.marcControl1);
            AppUtility.LoadMarc(this.marcControl1);
        }

        static UnitInfo FindPath(UnitInfo root,
            UnitNode[] path)
        {
            var current = root;
            Debug.Assert(path[0].Type == UnitType.Record);
            if (root.Type != path[0].Type)
                return null;
            foreach (var node in path.Skip(1))
            {
                var multi = current.SubUnits.Where(o => o.Name == node.Name);
                if (multi.Count() == 0)
                    return null;
                // 从多个中选出一个来
                current = filter(multi, node);
            }
            return current;

            UnitInfo filter(IEnumerable<UnitInfo> list, UnitNode path_node)
            {
                // TODO: 可以考虑根据 UnitInfo.Sensitive 判断是否需要经过内容过滤
                if (path_node.Type == UnitType.Field
                    && path_node.Name == "006")
                {
                    var type_name = get_006_type(path_node.GetContent(path_node.Box));
                    return list.Where(o => o.Caption?.ToLower().Contains(type_name.ToLower()) ?? false).FirstOrDefault();
                }

                return list.FirstOrDefault();
            }
        }

        static string get_006_type(string eValue)
        {
            string strType = "";

            if (eValue == null || eValue.Length < 1)
            {
                // 权且当作 'a' 处理
                strType = "Books";
            }
            else
            {
                // http://www.loc.gov/marc/bibliographic/bd006.html
                // a - Language material
                // Coded data elements relating to nonserial language material.
                if (eValue[0] == 'a')
                    strType = "Books";
                // c - Notated music
                // Coded data elements relating to notated music.
                else if (eValue[0] == 'c')
                    strType = "Music";
                // d - Manuscript notated music
                // Coded data elements relating to manuscript notated music.
                else if (eValue[0] == 'd')
                    strType = "Music";
                // e - Cartographic material
                // Coded data elements relating to nonmanuscript cartographic material.
                else if (eValue[0] == 'e')
                    strType = "Maps";
                // f - Manuscript cartographic material
                // Coded data elements relating to manuscript cartographic material.
                else if (eValue[0] == 'f')
                    strType = "Maps";
                // g - Projected medium
                // Coded data elements relating to a projected medium.
                else if (eValue[0] == 'g')
                    strType = "Visual Materials";
                // i - Nonmusical sound recording
                // Coded data elements relating to a nonmusical sound recording.
                else if (eValue[0] == 'i')
                    strType = "Music";
                // j - Musical sound recording
                // Coded data elements relating to a musical sound recording.
                else if (eValue[0] == 'j')
                    strType = "Music";
                // k - Two-dimensional nonprojectable graphic
                // Coded data elements relating to a two-dimensional nonprojectable graphic.
                else if (eValue[0] == 'k')
                    strType = "Visual Materials";
                // m - Computer file/Electronic resource
                // Coded data elements relating to either a computer file or an electronic resource in form.
                else if (eValue[0] == 'm')
                    strType = "Computer Files";
                // o - Kit
                // Coded data elements relating to a kit.
                else if (eValue[0] == 'o')
                    strType = "Visual Materials";
                // p - Mixed material
                // Coded data elements relating to mixed material.
                else if (eValue[0] == 'p')
                    strType = "Mixed Materials";
                // r - Three-dimensional artifact or naturally occurring object
                // Coded data elements relating to a three-dimensional artifact or naturally occurring object.
                else if (eValue[0] == 'r')
                    strType = "Visual Materials";
                // s - Serial/Integrating resource
                // Coded data elements relating to the control aspects of a non-printed continuing resource. For serially-controlled printed language material, field 008 is used.
                else if (eValue[0] == 's')
                    strType = "Continuing Resources";
                // t - Manuscript language material
                // Coded data elements relating to manuscript language material.
                else if (eValue[0] == 't')
                    strType = "Books";
                else
                {
                    // "无法根据当前006字段第一字符内容 '" + eValue[0].ToString() + "' 判断模板类型";
                    return "Books";
                }
            }

            return strType;
        }

        static void CutLevel(UnitInfo info, int level)
        {
            if (level <= 1)
                info.SubUnits = null;
            else
            {
                foreach (var child in info.SubUnits)
                {
                    CutLevel(child, level - 1);
                }
            }
        }

        static UnitInfo BuildTree(string marc_syntax)
        {
            if (marc_syntax == "unimarc")
                return BuildUnimarcTree();
            if (marc_syntax == "usmarc")
                return BuildUsmarcTree();
            throw new ArgumentException($"无法识别的 marc_syntax '{marc_syntax}'");
        }

        static UnitInfo BuildUnimarcTree()
        {
            var info_100 = UnitInfo.FromSubfields("100");
            info_100.SubUnits.RemoveAt(0);
            info_100.SubUnits.Insert(0, UnitInfo.FromChars(UnitType.Subfield, "a", new int[] { 2, 3, 5, 10 }));
            return new UnitInfo
            {
                Type = UnitType.Record,
                SubUnits = new List<UnitInfo>
                {
                    UnitInfo.FromChars(UnitType.Field,
                            "###",
                            new int[] { 5, 3, 5, 10 }),
                    UnitInfo.FromChars(UnitType.Field,
                            "001",
                            new int[] { 2, 3, 5, 10 }),
                    info_100,
                    UnitInfo.FromSubfields("200"),
                    new UnitInfo
                    {
                        Type = UnitType.Field,
                        Name = "410",
                        Caption = "里面会嵌套字段",
                        SubUnits = new List<UnitInfo>
                        {
                            UnitInfo.FromChars(UnitType.Field,
                                "001",
                                new int[] { 2, 3, 5, 10 }),
                            info_100,
                            UnitInfo.FromSubfields("200"),
                        },
                    }
                },
            };
        }

        /*
  <Field name="006" type="Books" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Books</Label>
      <Label xml:lang="zh">附件特征 - 图书</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/4">
      <Property>
        <Label xml:lang="en">Illustrations</Label>
        <Label xml:lang="zh">图表</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_1/4" />
      </Property>
    </Char>
    <Char name="5/1">
      <Property>
        <Label xml:lang="en">Target audience</Label>
        <Label xml:lang="zh">读者对象</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#targetaudiencecodes" />
      </Property>
    </Char>
    <Char name="6/1">
      <Property>
        <Label xml:lang="en">Form of item</Label>
        <Label xml:lang="zh">载体形态</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="7/4">
      <Property>
        <Label xml:lang="en">Nature of contents</Label>
        <Label xml:lang="zh">内容特征</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_7/4" />
      </Property>
    </Char>
    <Char name="11/1">
      <Property>
        <Label xml:lang="en">Government publication</Label>
        <Label xml:lang="zh">政府出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#governmentpublicationcodes" />
      </Property>
    </Char>
    <Char name="12/1">
      <Property>
        <Label xml:lang="en">Conference publication</Label>
        <Label xml:lang="zh">会议出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_12/1" />
      </Property>
    </Char>
    <Char name="13/1">
      <Property>
        <Label xml:lang="en">Festschrift</Label>
        <Label xml:lang="zh">纪念文集</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_13/1" />
      </Property>
    </Char>
    <Char name="14/1">
      <Property>
        <Label xml:lang="en">Index</Label>
        <Label xml:lang="zh">索引</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#indexcodes" />
      </Property>
    </Char>
    <Char name="15/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_15/1" />
      </Property>
    </Char>
    <Char name="16/1">
      <Property>
        <Label xml:lang="en">Literary form</Label>
        <Label xml:lang="zh">文学体裁</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_16/1" />
      </Property>
    </Char>
    <Char name="17/1">
      <Property>
        <Label xml:lang="en">Biography</Label>
        <Label xml:lang="zh">传记</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006book_17/1" />
      </Property>
    </Char>
  </Field>
  <Field name="006" type="Computer Files" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Computer files/Electronic resources</Label>
      <Label xml:lang="zh">附件特征 - 计算机文件/电子资源</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/4">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006computer_1/4" />
      </Property>
    </Char>
    <Char name="5/1">
      <Property>
        <Label xml:lang="en">Target audience</Label>
        <Label xml:lang="zh">读者对象</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#targetaudiencecodes" />
      </Property>
    </Char>
    <Char name="6/3">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006computer_6/3" />
      </Property>
    </Char>
    <Char name="9/1">
      <Property>
        <Label xml:lang="en">Type of computer file</Label>
        <Label xml:lang="zh">计算机文件类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006computer_9/1" />
      </Property>
    </Char>
    <Char name="10/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006computer_10/1" />
      </Property>
    </Char>
    <Char name="11/1">
      <Property>
        <Label xml:lang="en">Government publication</Label>
        <Label xml:lang="zh">政府出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#governmentpublicationcodes" />
      </Property>
    </Char>
    <Char name="12/6">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006computer_12/1" />
      </Property>
    </Char>
  </Field>
  <Field name="006" type="Maps" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Maps</Label>
      <Label xml:lang="zh">附件特征 - 地图</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/4">
      <Property>
        <Label xml:lang="en">Relief</Label>
        <Label xml:lang="zh">地形</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_1/4" />
      </Property>
    </Char>
    <Char name="5/2">
      <Property>
        <Label xml:lang="en">Projection</Label>
        <Label xml:lang="zh">投影</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_5/2" />
      </Property>
    </Char>
    <Char name="7/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_7/1" />
      </Property>
    </Char>
    <Char name="8/1">
      <Property>
        <Label xml:lang="en">Type of cartographic material</Label>
        <Label xml:lang="zh">测绘制图资料出版形式</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_8/1" />
      </Property>
    </Char>
    <Char name="9/2">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_9/2" />
      </Property>
    </Char>
    <Char name="11/1">
      <Property>
        <Label xml:lang="en">Government publication</Label>
        <Label xml:lang="zh">政府出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#governmentpublicationcodes" />
      </Property>
    </Char>
    <Char name="12/1">
      <Property>
        <Label xml:lang="en">Form of item</Label>
        <Label xml:lang="zh">载体形态</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="13/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_13/1" />
      </Property>
    </Char>
    <Char name="14/1">
      <Property>
        <Label xml:lang="en">Index</Label>
        <Label xml:lang="zh">索引</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#indexcodes" />
      </Property>
    </Char>
    <Char name="15/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_15/1" />
      </Property>
    </Char>
    <Char name="16/2">
      <Property>
        <Label xml:lang="en">Special format characteristics</Label>
        <Label xml:lang="zh">特殊形式特征</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006maps_16/2" />
      </Property>
    </Char>
  </Field>
  <Field name="006" type="Music" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Music</Label>
      <Label xml:lang="zh">附件特征 - 音乐</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/2">
      <Property>
        <Label xml:lang="en">Form of composition</Label>
        <Label xml:lang="zh">乐曲形式</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_1/2" />
      </Property>
    </Char>
    <Char name="3/1">
      <Property>
        <Label xml:lang="en">Format of music</Label>
        <Label xml:lang="zh">乐谱类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_3/1" />
      </Property>
    </Char>
    <Char name="4/1">
      <Property>
        <Label xml:lang="en">Music parts</Label>
        <Label xml:lang="zh">分谱</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_4/1" />
      </Property>
    </Char>
    <Char name="5/1">
      <Property>
        <Label xml:lang="en">Target audience</Label>
        <Label xml:lang="zh">读者对象</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#targetaudiencecodes" />
      </Property>
    </Char>
    <Char name="6/1">
      <Property>
        <Label xml:lang="en">Form of item</Label>
        <Label xml:lang="zh">载体形态</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="7/6">
      <Property>
        <Label xml:lang="en">Accompanying matter</Label>
        <Label xml:lang="zh">附件</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_7/6" />
      </Property>
    </Char>
    <Char name="13/2">
      <Property>
        <Label xml:lang="en">Literary text for sound recordings</Label>
        <Label xml:lang="zh">录音资料的文体类别</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_13/2" />
      </Property>
    </Char>
    <Char name="15/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_15/1" />
      </Property>
    </Char>
    <Char name="16/1">
      <Property>
        <Label xml:lang="en">Transposition and arrangement</Label>
        <Label xml:lang="zh">变调和改编</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_16/1" />
      </Property>
    </Char>
    <Char name="17/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006music_17/1" />
      </Property>
    </Char>
  </Field>
  <Field name="006" type="Continuing Resources" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Continuing resources</Label>
      <Label xml:lang="zh">附件特征 - 连续性资源</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/1">
      <Property>
        <Label xml:lang="en">Frequency</Label>
        <Label xml:lang="zh">出版频率</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_1/1" />
      </Property>
    </Char>
    <Char name="2/1">
      <Property>
        <Label xml:lang="en">Regularity</Label>
        <Label xml:lang="zh">发行规律</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_2/1" />
      </Property>
    </Char>
    <Char name="3/1">
      <Property>
        <Label xml:lang="en">ISSN center</Label>
        <Label xml:lang="zh">ISSN 中心</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_3/1" />
      </Property>
    </Char>
    <Char name="4/1">
      <Property>
        <Label xml:lang="en">Type of continuing resource</Label>
        <Label xml:lang="zh">连续性资源类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_4/1" />
      </Property>
    </Char>
    <Char name="5/1">
      <Property>
        <Label xml:lang="en">Form of original item</Label>
        <Label xml:lang="zh">原版文献形式</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="6/1">
      <Property>
        <Label xml:lang="en">Form of item</Label>
        <Label xml:lang="zh">载体形态</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="7/1">
      <Property>
        <Label xml:lang="en">Nature of entire work</Label>
        <Label xml:lang="zh">整体特征</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_7/1" />
      </Property>
    </Char>
    <Char name="8/3">
      <Property>
        <Label xml:lang="en">Nature of contents</Label>
        <Label xml:lang="zh">内容特征</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#natureofcontentscodes" />
      </Property>
    </Char>
    <Char name="11/1">
      <Property>
        <Label xml:lang="en">Government publication</Label>
        <Label xml:lang="zh">政府出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#governmentpublicationcodes" />
      </Property>
    </Char>
    <Char name="12/1">
      <Property>
        <Label xml:lang="en">Conference publication</Label>
        <Label xml:lang="zh">会议出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_12/1" />
      </Property>
    </Char>
    <Char name="13/3">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_13/3" />
      </Property>
    </Char>
    <Char name="16/1">
      <Property>
        <Label xml:lang="en">Original alphabet or script of title</Label>
        <Label xml:lang="zh">题名原文字母或文字</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_16/1" />
      </Property>
    </Char>
    <Char name="17/1">
      <Property>
        <Label xml:lang="en">Entry convention</Label>
        <Label xml:lang="zh">款目原则</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006continuing_17/1" />
      </Property>
    </Char>
  </Field>
  <Field name="006" type="Mixed Materials" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Mixed materials</Label>
      <Label xml:lang="zh">附件特征 - 混合型资料</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/5">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006mixed_1/5" />
      </Property>
    </Char>
    <Char name="6/1">
      <Property>
        <Label xml:lang="en">Form of item</Label>
        <Label xml:lang="zh">载体形态</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="7/11">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006mixed_7/11" />
      </Property>
    </Char>
  </Field>
  <Field name="006" type="Visual Materials" mandatory="yes" repeatable="no">
    <Property>
      <Label xml:lang="en">Additional Material Characteristics -- Visual materials</Label>
      <Label xml:lang="zh">附件特征 - 可视资料</Label>
    </Property>
    <Char name="0/1">
      <Property>
        <Label xml:lang="en">Form of material</Label>
        <Label xml:lang="zh">资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006_0/1" />
        <sensitive />
      </Property>
    </Char>
    <Char name="1/3">
      <Property>
        <Label xml:lang="en">Running time</Label>
        <Label xml:lang="zh">放映时间</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_1/3" />
      </Property>
    </Char>
    <Char name="4/1">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_4/1" />
      </Property>
    </Char>
    <Char name="5/1">
      <Property>
        <Label xml:lang="en">Target audience</Label>
        <Label xml:lang="zh">读者对象</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_5/1" />
      </Property>
    </Char>
    <Char name="6/5">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_6/5" />
      </Property>
    </Char>
    <Char name="11/1">
      <Property>
        <Label xml:lang="en">Government publication</Label>
        <Label xml:lang="zh">政府出版物</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#governmentpublicationcodes" />
      </Property>
    </Char>
    <Char name="12/1">
      <Property>
        <Label xml:lang="en">Form of item</Label>
        <Label xml:lang="zh">载体形态</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#formofitemcodes" />
      </Property>
    </Char>
    <Char name="13/3">
      <Property>
        <Label xml:lang="en">Undefined</Label>
        <Label xml:lang="zh">未定义</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_13/3" />
      </Property>
    </Char>
    <Char name="16/1">
      <Property>
        <Label xml:lang="en">Type of visual material</Label>
        <Label xml:lang="zh">可视资料类型</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_16/1" />
      </Property>
    </Char>
    <Char name="17/1">
      <Property>
        <Label xml:lang="en">Technique</Label>
        <Label xml:lang="zh">技术</Label>
        <Help xml:lang="zh" />
        <ValueList ref="marcvaluelist#006visual_17/1" />
      </Property>
    </Char>
  </Field>
         * */
        static UnitInfo BuildUsmarcTree()
        {
            var ret = new UnitInfo
            {
                Type = UnitType.Record,
                SubUnits = new List<UnitInfo>
                {
                    Build006_books(),   // 第一字符 a
                    Build006_computerFiles(),   // 第一字符 m
                },
            };
            return ret;
        }

        static UnitInfo Build006_books()
        {
            return new UnitInfo
            {
                Name = "006",
                Caption = "Additional Material Characteristics -- Books",
                Type = UnitType.Field,
                Sensitive = true,
                SubUnits = new List<UnitInfo>
                {
                    new UnitInfo
                    {
                        Name = "0/1",
                        Caption = "Form of material",
                        Length = 1,
                        Type = UnitType.Chars,
                        Sensitive = true,
                    },
                    new UnitInfo
                    {
                        Name = "1/4",
                        Caption = "Illustrations",
                        Length = 4,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "5/1",
                        Caption = "Target audience",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "6/1",
                        Caption = "Form of item",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "7/4",
                        Caption = "Nature of contents",
                        Length = 4,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "11/1",
                        Caption = "Government publication",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "12/1",
                        Caption = "Conference publication",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "13/1",
                        Caption = "Festschrift",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "14/1",
                        Caption = "Index",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "15/1",
                        Caption = "Undefined",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "16/1",
                        Caption = "Literary form",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "17/1",
                        Caption = "Biography",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                },
            };
        }

        static UnitInfo Build006_computerFiles()
        {
            return new UnitInfo
            {
                Name = "006",
                Caption = "Additional Material Characteristics -- Computer files/Electronic resources",
                Type = UnitType.Field,
                Sensitive = true,
                SubUnits = new List<UnitInfo>
                {
                    new UnitInfo
                    {
                        Name = "0/1",
                        Caption = "Form of material",
                        Length = 1,
                        Type = UnitType.Chars,
                        Sensitive = true,
                    },
                    new UnitInfo
                    {
                        Name = "1/4",
                        Caption = "Undefined",
                        Length = 4,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "5/1",
                        Caption = "Target audience",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "6/3",
                        Caption = "Undefined",
                        Length = 3,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "9/1",
                        Caption = "Type of computer file",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "10/1",
                        Caption = "Undefined",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "11/1",
                        Caption = "Government publication",
                        Length = 1,
                        Type = UnitType.Chars,
                    },
                    new UnitInfo
                    {
                        Name = "12/6",
                        Caption = "Undefined",
                        Length = 6,
                        Type = UnitType.Chars,
                    },
                },
            };
        }


        IEnumerable<ValueItem> FindValueList(UnitNode[] path)
        {
            var text = ToString(path);
            switch (text)
            {
                case "###|(0/5)":
                    return new List<ValueItem>
                    {
                        new ValueItem {
                            Value = "11",
                            Comment = "comment 11"
                        },
                        new ValueItem {
                            Value = "22",
                            Comment = "22", //"comment 22 测试 非常长的文字 test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test test "
                        },
                        new ValueItem {
                            Value = "  ",
                            Comment = "两个空格"
                        },
                        new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },
                                                new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },                        new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },                        new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },                        new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },                        new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },
                        new ValueItem {
                            Value = "33",
                            Comment = "注释"
                        },
                        new ValueItem {
                            Value = "99",
                            Comment = "注释"
                        },
                    };
                case "100|a|(0/2)":
                case "410|100|a|(0/2)":
                    return new List<ValueItem>
                    {
                        new ValueItem {
                            Value = "11",
                            Comment = "comment 11"
                        },
                        new ValueItem {
                            Value = "22",
                            Comment = "comment 22"
                        },

                    };
            }

            return null;
        }

        static string ToString(UnitNode[] path)
        {
            var text = new StringBuilder();
            foreach (var node in path)
            {
                if (text.Length > 0)
                    text.Append("|");
                text.Append(node.Name);
            }

            return text.ToString();
        }

        private void MenuItem_testSetCallback_Click(object sender, EventArgs e)
        {
            var context = this.marcControl1.GetContext();
            context.GetBackColor = (range, highlight) =>
            {
                if (highlight)
                    return Color.DarkRed;
                return Color.White;
            };
            context.GetForeColor = (o, highlight) =>
            {
                if (highlight)
                    return Color.White;
                var range = o as Range;
                if (range.Tag is bool)
                    return Color.DarkRed; // 子字段名文本为红色
                return Color.Black;
            };
            context.SplitRange = (o, content) =>
            {
                return SimpleText.SegmentSubfields2(content, '\x001f', 2, true);
            };

            // 迫使重新生成结构
            var save = this.marcControl1.Content;
            this.marcControl1.Content = "";
            this.marcControl1.Content = save;
            MessageBox.Show(this, "context GetBackColor() GetForeColor() 已经被设置为定制效果:\r\n普通文字白底黑字(子字段符号为红色)；选择文字红底白字");
        }

        // 将切割好的一个一个子字段字符串，的每一个，进一步切割为 name 和 content 两部分
        string[] SplitNameContent(string[] subfields)
        {
            List<string> results = new List<string>();
            foreach (var subfield in subfields)
            {
                if (subfield.Length > 2
                    && subfield.StartsWith("\x001f")
                    )
                {
                    results.Add(subfield.Substring(0, 2));
                    results.Add(subfield.Substring(2));
                }
                else
                    results.Add(subfield);
            }

            return results.ToArray();
        }

        private void MenuItem_clearCallback_Click(object sender, EventArgs e)
        {
            var context = this.marcControl1.GetContext();
            context.GetBackColor = null;
            context.GetForeColor = null;
            context.SplitRange = null;

            // 迫使重新生成结构
            this.marcControl1.Content = this.marcControl1.Content;
            MessageBox.Show(this, "context GetBackColor() GetForeColor() 已经被设置为 null");
        }

        private void MenuItem_apperance_DropDownOpening(object sender, EventArgs e)
        {
            this.MenuItem_readonly.Checked = this.marcControl1.ReadOnly;
        }

        private void MenuItem_readonly_Click(object sender, EventArgs e)
        {
            this.marcControl1.ReadOnly = !this.marcControl1.ReadOnly;
        }

        private void MenuItem_dumpHistory_Click(object sender, EventArgs e)
        {
            string strFileName = Path.Combine(AppUtility.GetBinDirectory(), "history.txt");
            File.WriteAllText(strFileName, this.marcControl1.DumpHistory());
            Process.Start("notepad.exe", strFileName);
        }



        public void SetFont()
        {
            FontDialog dlg = new FontDialog();
            dlg.ShowColor = true;
            //dlg.Color = this.marcControl1.ContentTextColor;
            dlg.Font = this.marcControl1.Font;
            dlg.ShowApply = true;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            //dlg.Apply -= new EventHandler(dlgMarcEditFont_Apply);
            //dlg.Apply += new EventHandler(dlgMarcEditFont_Apply);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            this.marcControl1.Font = dlg.Font;
            //this.marcControl1.ContentTextColor = dlg.Color;
        }

        private void MenuItem_setFont_Click(object sender, EventArgs e)
        {
            SetFont();
        }

        private void MenuItem_verifyCharCount_Click(object sender, EventArgs e)
        {
            // 按下 Ctrl 键可自动修复
            var fix = (Control.ModifierKeys & Keys.Control) != 0;
            var errors = this.marcControl1.Verify(fix);
            MessageBox.Show(
                this,
                string.Join("\r\n", errors)
                + (fix && errors.Count() > 0 ? "\r\n已经自动修复。" : ""));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
            AppUtility.SaveMarc(this.marcControl1);
            AppUtility.SaveState(this.marcControl1);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void MenuItem_startStressTest_Click(object sender, EventArgs e)
        {
            StartClipboardStressTest();
        }

        private void MenuItem_stopStressTest_Click(object sender, EventArgs e)
        {
            Stop();
        }

        #region Clipboard Stress Testing

        CancellationTokenSource _cancel = null;

        void StartClipboardStressTest()
        {
            Stop();
            _cancel = new CancellationTokenSource();
            var token = _cancel.Token;
            var task = Task.Factory.StartNew((o) =>
            {
                try
                {
                    for (int i = 0; ; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        // TODO: 为了加大压力，是否应该用复制图像进入 Clipboard 来测试
                        var ret = CopyTextToClipboard($"test {i}");
                        /*
                        this.Invoke(new Action(() =>
                        {
                            Clipboard.SetText($"test {i}");
                        }));
                        */
                        Thread.Sleep(20);
                    }
                }
                catch (Exception ex)
                {
                    int k = 0;
                    k++;
                }
            },
            null,
            default,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        void Stop()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
                _cancel.Dispose();
                _cancel = null;
            }
        }


        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [DllImport("user32.dll")]
        private static extern bool SetClipboardData(uint uFormat, IntPtr data);
        private const uint CF_UNICODETEXT = 13;
        public static bool CopyTextToClipboard(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }
            var global = Marshal.StringToHGlobalUni(text);
            var ret = SetClipboardData(CF_UNICODETEXT, global);
            CloseClipboard();
            return ret;
        }

        #endregion


        // 测试随机发生的字符串
        private void MenuItem_test_randomChars_Click(object sender, EventArgs e)
        {
            char startChar = (char)0x20;
            int count = 1000;

            for (int i = 0; i < 10; i++)
            {
                var text = BuildTestContent(startChar, count);
                this.marcControl1.Content = text.ToString();
                startChar += (char)count;
                if (startChar > 0x9FA5)
                    startChar = (char)0x20;

                Thread.Sleep(500);
            }
        }

        static string BuildTestContent(char startChar, int count)
        {
            var text = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var ch = (char)((int)startChar + i);
                text.Append(ch);
                if ((i % 1000) == 0)
                    text.Append((char)30);
            }

            return text.ToString();
        }

        private async void MenuItem_test_loadFromCompactFile_Click(object sender, EventArgs e)
        {
            Stop();
            _cancel = new CancellationTokenSource();
            var token = _cancel.Token;

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = $"Open compact file";
                dlg.Filter = "*.compact|*.compact|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                await Task.Factory.StartNew((t) =>
                    {
                        int i = 0;
                        foreach (var content in CompactReader(dlg.FileName, Encoding.UTF8))
                        {
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            this.Invoke(new Action(() =>
                            {
                                this.marcControl1.Content = content;
                                this.marcControl1.Update();
                            }));
                            SetMessage((++i).ToString());
                            // Thread.Sleep(500);
                        }
                    },
                    null,
                    token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        static IEnumerable<string> CompactReader(string filename,
            Encoding encoding)
        {
            using (var stream = File.OpenRead(filename))
            using (var s = new BufferedStream(stream))
            {
                List<byte> buffer = new List<byte>();
                while (true)
                {
                    var ret = s.ReadByte();
                    if (ret == -1)
                    {
                        if (buffer.Count > 0)
                        {
                            yield return encoding.GetString(buffer.ToArray());
                        }

                        yield break;
                    }
                    buffer.Add((byte)ret);
                    if (ret == 29)
                    {
                        yield return Encoding.UTF8.GetString(buffer.ToArray());
                        buffer.Clear();
                    }
                }
            }
        }

        void SetMessage(string text)
        {
            this.Invoke(new Action(() =>
            {
                toolStripStatusLabel_message.Text = text;
            }));
        }
    }

}
