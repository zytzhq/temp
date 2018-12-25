using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using wall;
using System.IO;

namespace zyt.Image
{
    public class SDataColor : MonoBehaviour
    {
        public int rows = 32;//贴图行数
        public int blocks = 8;//贴图分割行数
        public float speed = 5.0f;//速度
        public int upset = 10;//要打乱的数量
        public int templateNums = 4;//模板图数量
        public int imageNums = 5000;//贴图数量
        public int screenW = 6480;//屏幕宽
        public int screenH = 1920;//屏幕高
        public string localPath = "c:/Users/Administrator/Desktop/testImage/";//绝对路径
        public static bool isImageMatch = false;//功能可用标记
        private bool isMove = false;

        //模板图
        private List<Texture2D> majorImages = new List<Texture2D>();
        //控制
        private int moveSwitch = 0;
        private static int useMajorNum = 0;
        private static int useMajorNumTemp = -1;
        //贴图与数据获取
        private List<Sprite> dataImage = new List<Sprite>();//贴图单位方块颜色
        private List<Color[]> colorDataImage = new List<Color[]>();//贴图单位方块颜色
        private int[] colorNumTemp;
        //贴图对应
        public Transform[] grandFa;
        private Transform[] useImage;//记录所有图片,数组存入缓存 

        private int[] colorNum;
        private List<Color[]> colorImage = new List<Color[]>();//获取对应颜色
        private float sonRectUnit = 0;
        private List<int> wImageTex = new List<int>();//图片像素宽度
        private List<float> wImageRect = new List<float>();//图片宽
        private List<float> hImageRect = new List<float>();//图片高
        private float ratio = 0;//比例
        //位移
        private float xFa, yFa;//起始坐标
        //模版图
        private List<Color[]> majorColorImage = new List<Color[]>();//模版图单位方块颜色
        private int majorNum = 0;//模版图计数
        private int column;//列数
        private int rowM;//真实行数
        private float hUnit, wUnit;//标准单位 
        private int wSum = 0, hSum = 0;
        private int yF = 0;
        //铺满背景
        private float[] rowEndX;
        //输出数据——多张模版图片
        private static bool switchReadSonData = true;
        private int[,] useTag;
        private Vector2[,] targetPos;
        private Vector2[] imageSize;//记录宽高
        //缩放比
        private float zoom = 19.2f;

        void Start()
        {
            //DataBase.LoadDataFromAssetBundle("F:/ABs/");
            isImageMatch = false;
            isMove = false;
            ReadFolderImage(localPath);
            CreateSpriteRank(imageNums);
            ImageSetInit();
        }

        /// <summary>
        /// 创建空白spriteRenderer
        /// </summary>
        private void CreateSpriteRank(int imageNum)
        {
            bool isSon = true;
            foreach (Transform child in gameObject.transform)
            {
                if (child == null)
                {
                    break;
                }
                else
                {
                    isSon = false;
                    break;
                }
            }
            if (isSon)
            {
                for (int i = 0; i < imageNum - 1; i++)
                {
                    GameObject son = new GameObject();
                    son.name = "spriteImage" + i;
                    son.AddComponent<SpriteRenderer>();
                    son.transform.parent = this.gameObject.transform;
                }
            }
        }

        /// <summary>
        /// 获取指定路径文件夹下所有图片
        /// </summary>
        /// <param name="templateNum">要用得模板图数量</param>
        /// <param name="path">绝对路径</param>
        private void ReadFolderImage(string path)
        {
            List<string> filePaths = new List<string>();
            string imgtype = "*.JPG|*.PNG";
            string[] ImageType = imgtype.Split('|');
            for (int i = 0; i < ImageType.Length; i++)
            {
                //获取某文件夹下所有的图片路径
                string[] dirs = Directory.GetFiles(path, ImageType[i]);//@""+
                for (int j = 0; j < dirs.Length; j++)
                {
                    filePaths.Add(dirs[j]);
                }
            }

            if (filePaths.Count == 0)
                Debug.LogError("指定路径文件夹内无图片");

            //WWW方式获取Texture2D
            if (filePaths.Count < templateNums)
            {
                for (int i = 0; i < filePaths.Count; i++)
                {
                    StartCoroutine(GetImageWWW(filePaths[i]));
                }
                templateNums = filePaths.Count;//模板图过少
            }
            else
            {
                for (int i = 0; i < templateNums; i++)
                {
                    StartCoroutine(GetImageWWW(filePaths[i]));
                }
            }
        }

        private IEnumerator GetImageWWW(string url)
        {
            WWW www = new WWW(@"file:///" + url);
            //Debug.Log(url);
            while (!www.isDone)
                yield return www;
            //获取Texture
            Texture2D tex2D = (Texture2D)www.texture;
            majorImages.Add(tex2D);
            //清理之前数据，释放内存
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 初始化设置
        /// </summary>
        private void ImageSetInit()
        {
            if (blocks < 1)
                blocks = 1;
            if (rows < 1)
                rows = 1;
            if (upset < 1)
                upset = 1;
            useMajorNum = 0;
            useMajorNumTemp = -1;
            switchReadSonData = true;
        }

        /// <summary>
        /// 协程控制
        /// </summary>
        private void ImageInit()
        {
            if (useMajorNum < templateNums)
            {
                StartCoroutine(ImageProcess(rows, blocks, upset, templateNums, imageNums, screenW, screenH, majorImages[useMajorNum]));
            }
            else if (useMajorNum == templateNums)
            {
                wImageRect.Clear();
                hImageRect.Clear();
                wImageTex.Clear();
                colorDataImage.Clear();
                dataImage.Clear();
                majorColorImage.Clear();
                colorImage.Clear();
                colorNum = null;
                colorNumTemp = null;
                //grandFa = null;

                StopAllCoroutines();
                useMajorNum++;
            }
        }

        /// <summary>
        /// 自适应图片拼图
        /// </summary>
        /// <param name="row">拼图行数，使用时输入2的次方</param>
        /// <param name="block">单张拼图色块分割行数，使用应输入2的次方</param>
        /// <param name="upsetNum">设置相邻几张图片不相同</param>
        /// <param name="templateNum">模版图数量</param>
        /// <param name="imageNum">贴图数量</param>
        /// <param name="screenW">显示屏幕宽</param>
        /// <param name="screenH">显示屏幕高，模版图不会拉伸</param>
        /// <param name="bitmap">被拼图像，bitmap.height应大于等于row*block</param>
        private IEnumerator ImageProcess(int row, int block, int upsetNum, int templateNum, int imageNum, int screenW, int screenH, Texture2D bitmap)
        {
            #region 模版图像素颜色数组初始化
            rowM = row * block;
            while (bitmap.height < rowM)
            {
                Debug.LogError(string.Format("row * block过大:{0}", rowM));
                block--;
                rowM = row * block;
            }
            hUnit = bitmap.height / (float)rowM;
            wUnit = hUnit;
            
            column = (int)(bitmap.width / wUnit);//单位方块为正方形    
            majorColorImage.Add(new Color[block * column * row]);
            majorNum = 0;
            float sum_r = 0, sum_g = 0, sum_b = 0;
            for (int k = 0; k < row; k++)
            {
                yF = (int)(block * hUnit * k);//控制大列的y偏移
                for (int i = 0; i < column; i++)
                {
                    wSum = (int)(i * wUnit);//起始x变化值
                    for (int j = 0; j < block; j++)
                    {
                        sum_r = 0;
                        sum_g = 0;
                        sum_b = 0;
                        hSum = (int)(j * hUnit);//每大列里的分割块起始y变化值
                        var colorTemp = bitmap.GetPixels(wSum, yF + hSum, (int)wUnit, (int)hUnit);
                        foreach (var rgb in colorTemp)
                        {
                            sum_r += rgb.r;
                            sum_g += rgb.g;
                            sum_b += rgb.b;
                        }
                        majorColorImage[useMajorNum][majorNum] = new Color(sum_r / colorTemp.Length, sum_g / colorTemp.Length, sum_b / colorTemp.Length);
                        majorNum++;
                    }
                }
                yield return 1;
            }
            #endregion

            #region 读取子物体并按比例缩放
            if (switchReadSonData)
            {
                switchReadSonData = false;//只读取一次资源图信息

                foreach (var reS in DataBase.relics)
                {
                    dataImage.Add(reS.sprite);
                }
                int dataImageNum = 0;

                grandFa = GetComponentsInChildren<Transform>();
                for (int i = 1; i < imageNum; i++)
                    grandFa[i].gameObject.SetActive(false);

                zoom = screenH / 100;

                //初始化标记
                useTag = new int[templateNum, imageNum + 1];
                targetPos = new Vector2[templateNum, imageNum + 1];

                //初始化数组    
                colorNumTemp = new int[dataImage.Count];
                useImage = new Transform[imageNum];
                colorNum = new int[imageNum];
                imageSize = new Vector2[imageNum];
                rowEndX = new float[row];//记录每行最后的位置x

                //获得父物体数据
                useImage[0] = null;
                colorNum[0] = 0;

                //给[0]赋值使顺序一致
                var arrZero = new Color[0];
                colorImage.Add(arrZero);
                wImageTex.Add(bitmap.width);
                wImageRect.Add(screenW);//比例应和模版图一致
                hImageRect.Add(screenH);

                //获得子物体数据
                for (int i = 0; i < dataImage.Count; i++)
                {
                    //获取第i张图颜色(block控制每张图要分割为多少份）
                    Texture2D tex2d = (Texture2D)dataImage[i].texture;//获得图片
                    wImageTex.Add(tex2d.width);//获得贴图宽
                    int hTex2d = tex2d.height;
                    float hUnitSon = (float)hTex2d / (float)block;//子物体单位正方形
                    int sonNum = 0;
                    int arrNum = (int)((wImageTex[i + 1] / hUnitSon) * block);
                    if (arrNum > 0)//避免图宽度小于单位宽度时报错
                    {
                        var arr = new Color[arrNum];//定义该图分割为几份
                        colorNumTemp[i] = arrNum;//记录份数
                        for (int k = 0; k < (int)(wImageTex[i + 1] / hUnitSon); k++)
                        {
                            wSum = (int)(k * hUnitSon);//起始x变化值
                            for (int j = 0; j < block; j++)//按竖存，每个block为一个单位
                            {
                                //颜色分块求平均
                                sum_r = 0;
                                sum_g = 0;
                                sum_b = 0;
                                hSum = (int)(j * hUnitSon);//每大列里的分割块起始y变化值   
                                var colorTemp = tex2d.GetPixels(wSum, hSum, (int)hUnitSon, (int)hUnitSon);
                                foreach (var rgb in colorTemp)
                                {
                                    sum_r += rgb.r;
                                    sum_g += rgb.g;
                                    sum_b += rgb.b;
                                }
                                arr[sonNum] = new Color(sum_r / colorTemp.Length, sum_g / colorTemp.Length, sum_b / colorTemp.Length);
                                sonNum++;
                            }
                            yield return 1;
                        }
                        colorDataImage.Add(arr);
                    }
                    else
                    {
                        var arr = new Color[block];
                        colorNumTemp[i] = block;
                        for (int j = 0; j < block; j++)//按竖存，每个block为一个单位
                        {
                            //颜色分块求平均
                            sum_r = 0;
                            sum_g = 0;
                            sum_b = 0;
                            hSum = (int)(j * hUnitSon);
                            var colorTemp = tex2d.GetPixels(0, hSum, wImageTex[i + 1], (int)hUnitSon);
                            foreach (var rgb in colorTemp)
                            {
                                sum_r += rgb.r;
                                sum_g += rgb.g;
                                sum_b += rgb.b;
                            }
                            arr[sonNum] = new Color(sum_r / colorTemp.Length, sum_g / colorTemp.Length, sum_b / colorTemp.Length);
                            sonNum++;

                        }
                        colorDataImage.Add(arr);
                        yield return 1;
                    }
                }
                sonRectUnit = hImageRect[0] / row / zoom;//缩放后高度  
                for (int i = 1; i < imageNum; i++)
                {                
                    //绑定spriteRenderer组件
                    grandFa[i].gameObject.GetComponent<SpriteRenderer>().sprite = dataImage[dataImageNum];
                    useImage[i] = grandFa[i].gameObject.GetComponent<Transform>();

                    //比例调整为高一致，并缩小zoom倍
                    hImageRect.Add(dataImage[dataImageNum].texture.height/ zoom);//获得图片高     
                    wImageRect.Add(dataImage[dataImageNum].texture.width/ zoom);//获得图片宽
                    ratio = wImageRect[i] / hImageRect[i];//宽高比
                    wImageRect[i] = sonRectUnit * ratio;//宽等比缩放
                    imageSize[i] = new Vector2(wImageRect[i] * 100, sonRectUnit * 100);//获得宽高,*100保证像素对应
                    useImage[i].localScale = new Vector3(sonRectUnit * 100 / dataImage[dataImageNum].texture.height, sonRectUnit * 100 / dataImage[dataImageNum].texture.height, 0);

                    //对应正确的颜色和数量
                    colorImage.Add(colorDataImage[dataImageNum]);
                    colorNum[i] = colorNumTemp[dataImageNum];
                    dataImageNum++;
                    if (dataImageNum >= dataImage.Count)
                    {
                        dataImageNum = 0;
                        yield return 1;
                    }
                }
            }
            #endregion

            #region 匹配位移目标
            //数量
            int sonColumn = 0;//记录被用图列数
            int colorI = 0;//记录第几张图片
            int endNum = 1;//最终用图数量，从一开始，[0]被用来存背景      
            float wSonSum = 0;//x偏移
            //位置
            int[] speedTag = new int[dataImage.Count + 1];//记录一种图在给的数据中有多少张
            int sNumTemp = (imageNum - 1) % dataImage.Count;//最后一个没图（[0]存的是模版图）
            for (int stI = 0; stI < (dataImage.Count + 1); stI++)
            {
                speedTag[stI] = imageNum / dataImage.Count;
                if (stI < (sNumTemp + 1))
                    speedTag[stI]++;
            }
            float[] grayTemp = new float[dataImage.Count + 1];//记录要参与排序的灰度值
            int grayTempNum = 1;
            //灰度      
            float sumSonGray = 0;
            int majorGrayNum = 0;//对比第几块的灰度
            //记录多个灰度，降低重复率
            float[] sonGray = new float[dataImage.Count + 1];
            int[] numColorI = new int[dataImage.Count + 1];//记录位置   
            int[] beforeColorI = new int[upsetNum];
            bool[] markRepeat = new bool[upsetNum];//记录是否重复
            //边缘图片序号
            int[] fillStartMark = new int[row];
            int[] fillEndMark = new int[row];
            //使图居中
            xFa = 0;
            yFa = 0;
            //if (screenH > bitmap.height)
            xFa += (screenW - ((float)screenH / (float)bitmap.height) * bitmap.width) / 2;
            xFa /= zoom;
            //颜色匹配
            for (int i = 0; i < row; i++)//图总数量不够会报错
            {
                wSonSum = 0;
                for (int colNum = 0; colNum < column; colNum += sonColumn)//加上第colorI个图的列数
                {
                    //检索出匹配的图片 
                    grayTempNum = 1;
                    for (int j = 1; j < (dataImage.Count + 1); j++)
                    {
                        bool bSwitch = false;
                        int bNum = 0;
                        sumSonGray = 0;
                        if (useTag[useMajorNum, j] == 0)
                        {
                            for (int k = 0; k < colorImage[j].Length; k += 1)//每次都要测因为对比的位置不同
                            {
                                if (majorGrayNum >= majorColorImage[useMajorNum].Length)//防止数组越界
                                {
                                    majorGrayNum -= 1;
                                    bNum = k - 1;//-1是因为在sumSonGray前面
                                    bSwitch = true;
                                    break;
                                }
                                sumSonGray += (Mathf.Abs(colorImage[j][k].r - majorColorImage[useMajorNum][majorGrayNum].r)
                                    + Mathf.Abs(colorImage[j][k].g - majorColorImage[useMajorNum][majorGrayNum].g)
                                    + Mathf.Abs(colorImage[j][k].b - majorColorImage[useMajorNum][majorGrayNum].b));

                                //sumSonGray += Mathf.Abs(colorImage[j][k].grayscale - majorColorImage[useMajorNum][majorGrayNum].grayscale);

                                //if ((sumSonGray/k) > 0.2 && k>10)//牺牲一定精度提高运算速度，使用时将此循环的1改为2成像和速度都更好
                                //{
                                //    bNum = k;
                                //    bSwitch = true;
                                //    break; 
                                //}
                                majorGrayNum += 1;
                            }


                            //跳出除数不同
                            if (!bSwitch)
                            {
                                var avgSonGray = sumSonGray / colorImage[j].Length;
                                sonGray[j] = avgSonGray;//每次j循环结束后会被覆盖

                                majorGrayNum -= colorImage[j].Length;//重置
                            }
                            else
                            {
                                var avgSonGray = sumSonGray / (bNum + 1);
                                sonGray[j] = avgSonGray;//每次j循环结束后会被覆盖

                                majorGrayNum -= bNum;//重置
                            }
                            //考虑重复率                                
                            numColorI[grayTempNum] = j;
                            grayTemp[grayTempNum] = sonGray[j];
                            grayTempNum++;
                        }
                    }
                    //排序
                    SelectionSort(grayTemp, numColorI, grayTempNum, upsetNum);

                    //与前面的对比看是否重复
                    for (int more = 0; more < upsetNum; more++)
                    {
                        markRepeat[more] = false;
                    }
                    if (upsetNum >= 1)//endNum-1 > upsetNum && 
                    {
                        int switchRepeat = 0;
                        for (int markI = 0; markI < upsetNum; markI++)
                        {
                            for (int markJ = 0; markJ < upsetNum; markJ++)
                            {
                                if (numColorI[markJ + 1] == (beforeColorI[markI] % colorDataImage.Count) && !markRepeat[markJ])//////可能有BUG
                                {
                                    markRepeat[markJ] = true;
                                    switchRepeat++;
                                    break;
                                }
                            }
                            if (switchRepeat >= upsetNum)
                                break;
                        }
                        if (switchRepeat >= upsetNum)//upsetNum个都与前面有重复，就用最小的
                        {
                            colorI = numColorI[1];                         
                        }
                        else
                        {
                            for (int morej = 0; morej < upsetNum; morej++)
                            {
                                if (!markRepeat[morej])
                                {
                                    colorI = numColorI[morej + 1];
                                    break;
                                }
                            }
                        }
                    }
                    //从后往前获取图片
                    var sTemp = colorI;
                    colorI += (speedTag[sTemp] - 1) * dataImage.Count;
                    speedTag[sTemp]--;
                    //绑定匹配的图片   
                    useTag[useMajorNum, colorI] = 1;
                    //获得目标位置                            
                    targetPos[useMajorNum, colorI] = new Vector2(xFa + (wImageRect[colorI] / 2 + wSonSum), yFa + (sonRectUnit / 2 + i * sonRectUnit));//得到对应位置图片的位移目标点
                    wSonSum += wImageRect[colorI];//顺序不能变，在后面

                    beforeColorI[endNum % upsetNum] = colorI;//记录前upsetNum个位置
                    endNum++;
                    sonColumn = colorNum[colorI] / block;//记录要增加的行距
                    if ((colNum + sonColumn) < column)
                        majorGrayNum += colorNum[colorI];//记录用了多少块颜色
                    else
                    {
                        majorGrayNum += ((column - colNum) * block);//跳出时只加到此行满的数量
                        rowEndX[i] = wSonSum;
                        //获得结束边缘图片序号
                        fillEndMark[i] = colorI;
                    }
                    //获得开始边缘图片序号
                    if (colNum == 0)
                        fillStartMark[i] = colorI;
                    //贴图越多求模越小
                    if (endNum % 3 == 0)
                    {
                        yield return 1;
                    }
                }       
            }

            //背景填充
            float rowStartX = 0;
            int useCounting = 1;
            int columnUpseet = 1;

            //左铺满
            for (int i = 0; i < row; i++)//图总数量不够会报错
            {
                grayTempNum = 1;
                for (int j = 1; j < (dataImage.Count + 1); j++)
                {
                    if (useTag[useMajorNum, j] == 0)
                    {
                        sumSonGray = 0;
                        majorGrayNum = 0;
                        //找接近边缘的颜色
                        for (int k = 0; k < colorImage[j].Length; k += 1)
                        {
                            if (k >= colorImage[fillStartMark[i]].Length)//避免越界
                                break;
                            sumSonGray += (Mathf.Abs(colorImage[j][k].r - colorImage[fillStartMark[i]][k].r)
                                    + Mathf.Abs(colorImage[j][k].g - colorImage[fillStartMark[i]][k].g)
                                    + Mathf.Abs(colorImage[j][k].b - colorImage[fillStartMark[i]][k].b));
                            //sumSonGray += Mathf.Abs(colorImage[j][k].grayscale - colorImage[fillStartMark[i]][k].grayscale);
                            majorGrayNum += 1;
                        }
                        //考虑重复率
                        numColorI[grayTempNum] = j;
                        grayTemp[grayTempNum] = sumSonGray / majorGrayNum;
                        grayTempNum++;
                    }
                }
                
                //排序
                SelectionSort(grayTemp, numColorI, grayTempNum, grayTempNum);
                //使相邻列不同
                useCounting = columnUpseet;
                rowStartX = 0;
                yield return 1;
                while (rowStartX < (xFa + (int)(100 / zoom)))
                {         
                    colorI = numColorI[useCounting];//不用第[0]个
                    //降低越界风险
                    if (useCounting > (grayTempNum / 3 * 2))
                        useCounting++;
                    else
                    {
                        if (useCounting > 20)
                            useCounting += Random.Range(3, 7);
                        else
                            useCounting += 3;
                    }
                    //从后往前获取图片
                    var sTemp = colorI;
                    colorI += (speedTag[sTemp] - 1) * dataImage.Count;
                    speedTag[sTemp]--;
                    //绑定匹配的图片   
                    useTag[useMajorNum, colorI] = 1;
                    //获得目标位置
                    targetPos[useMajorNum, colorI] = new Vector2(xFa - (wImageRect[colorI] / 2 + rowStartX), yFa + (sonRectUnit / 2 + i * sonRectUnit));//得到对应位置图片的位移目标点
                    rowStartX += wImageRect[colorI];
                }
                columnUpseet++;
                if (columnUpseet > 6)
                    columnUpseet = 1;
            }

            //右铺满
            useCounting = 1;
            for (int i = 0; i < row; i++)//图总数量不够会报错
            {
                grayTempNum = 1;
                for (int j = 1; j < (dataImage.Count + 1); j++)
                {
                    if (useTag[useMajorNum, j] == 0)
                    {
                        sumSonGray = 0;
                        majorGrayNum = 0;
                        //找接近边缘的颜色
                        for (int k = 0; k < colorImage[j].Length; k += 1)
                        {
                            if (k >= colorImage[fillEndMark[i]].Length)//避免越界
                                break;
                            sumSonGray += (Mathf.Abs(colorImage[j][k].r - colorImage[fillEndMark[i]][k].r)
                                    + Mathf.Abs(colorImage[j][k].g - colorImage[fillEndMark[i]][k].g)
                                    + Mathf.Abs(colorImage[j][k].b - colorImage[fillEndMark[i]][k].b));
                            //sumSonGray += Mathf.Abs(colorImage[j][k].grayscale - colorImage[fillEndMark[i]][k].grayscale);
                            majorGrayNum += 1;
                        }
                        //考虑重复率                                
                        numColorI[grayTempNum] = j;
                        grayTemp[grayTempNum] = sumSonGray / majorGrayNum;
                        grayTempNum++;
                    }
                }
                yield return 1;
                //排序
                SelectionSort(grayTemp, numColorI, grayTempNum, grayTempNum);
                //使相邻列不同
                useCounting = columnUpseet;
                while (rowEndX[i] < (screenW / zoom - xFa + (int)(100/zoom)))
                {
                    colorI = numColorI[useCounting];
                    //降低越界风险
                    if (useCounting > (grayTempNum / 3 * 2))
                        useCounting++;
                    else
                    {
                        if (useCounting > 20)
                            useCounting += Random.Range(3, 7);
                        else
                            useCounting += 3;
                    }
                    //从后往前获取图片
                    var sTemp = colorI;
                    colorI += (speedTag[sTemp] - 1) * dataImage.Count;
                    speedTag[sTemp]--;
                    //绑定匹配的图片   
                    try
                    {
                        useTag[useMajorNum, colorI] = 1;
                    }
                    catch
                    {
                        Debug.Log("useMajorNum=" + useMajorNum);
                        Debug.Log("colorI=" + colorI);
                    }
                    //获得目标位置
                    targetPos[useMajorNum, colorI] = new Vector2(xFa + (wImageRect[colorI] / 2 + rowEndX[i]), yFa + (sonRectUnit / 2 + i * sonRectUnit));//得到对应位置图片的位移目标点
                    rowEndX[i] += wImageRect[colorI];
                }
                columnUpseet++;
                if (columnUpseet > 6)
                    columnUpseet = 1;
            }
            #endregion
            useMajorNum++;

        }

        /// <summary>
        /// 选择排序，小到大排序upsetNum2次
        /// </summary>
        /// <param name="A">需排序灰度图</param>
        /// <param name="B">位置</param>
        /// <param name="n">总数</param>
        /// <param name="upsetNum2">打乱数</param>
        void SelectionSort(float[] A, int[] B, int n, int upsetNum2)
        {
            for (int i = 1; i < upsetNum2 + 1; i++) //要用多少个找多少次,数组从1开始
            {
                int min = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (A[j] < A[min])
                    {
                        min = j;
                    }
                }
                if (min != i)
                {

                    float temp = A[min];
                    A[min] = A[i];
                    A[i] = temp;

                    //记录的位置一起替换
                    int temp2 = B[min];
                    B[min] = B[i];
                    B[i] = temp2;

                }
            }
        }

        /// <summary>
        /// 移动到目标位置
        /// </summary>
        /// <param name="sp">速度</param>
        public void ImgMove(float sp)
        {
            for (int i = 1; i < useImage.Length; i++)
            {
                if (useTag[moveSwitch, i] == 0)
                {
                    useImage[i].gameObject.SetActive(false);//不用的图要隐藏
                    useImage[i].position = new Vector3(168, 50, 0);
                    /*useImage[i].position = Vector2.Lerp(useImage[i].position, new Vector3(3240, -300, 0), sp * Time.deltaTime);
                    if (Vector2.Distance(new Vector3(3240, -300, 0), useImage[i].position) < 10.0f)
                    {
                        useImage[i].position = new Vector3(3240, -300, 0);                          
                        //useImage[i].localScale = Vector3.zero;
                    }*/
                }
                if (useTag[moveSwitch, i] == 1)
                {
                    useImage[i].gameObject.SetActive(true);
                    useImage[i].position = Vector2.Lerp(useImage[i].position, targetPos[moveSwitch, i], sp * Time.deltaTime);
                    if (Vector2.Distance(targetPos[moveSwitch, i], useImage[i].position) < 1.0f)
                    {
                        useImage[i].position = targetPos[moveSwitch, i];
                    }
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                moveSwitch++;
                if (moveSwitch > useMajorNum || moveSwitch >= templateNums)
                    moveSwitch = 0;
            }

            //控制功能可否启用
            if (useMajorNum >= 1)//useMajorNum>=templateNums
            {
                if (!isImageMatch)
                    Debug.Log("功能可启用");
                isImageMatch = true;
            }
            
            if (isImageMatch)
            {
                if (Input.GetKeyDown(KeyCode.Q))//控制图片是否展示
                {
                    isMove = !isMove;
                }
                if (isMove)
                    ImgMove(speed);
                else
                {
                    for (int i = 1; i < imageNums; i++)
                        grandFa[i].gameObject.SetActive(false);
                }
            }

            //计算拼图位置
            if (useMajorNumTemp != useMajorNum && majorImages.Count != 0)
            {
                ImageInit();
                useMajorNumTemp = useMajorNum;
            }

        }

        public void DisImage()
        {
            useImage = null;
            useTag = null;
            targetPos = null;
            imageSize = null;
        }

    }
}