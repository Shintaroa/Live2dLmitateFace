using DlibFaceLandmarkDetector;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using OpenCVForUnity;
using OpenCVForUnityExample;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Live2D;

[RequireComponent(typeof(WebCamTextureToMatHelper))]
public sealed class HiyoriImitateTheFace : MonoBehaviour
{


    /// <summary>
    /// 脸的界标检测器
    /// </summary>
    FaceLandmarkDetector faceLandmarkDetector;

    /// <summary>
    /// 3d物体点
    /// </summary>
    MatOfPoint3f objectPoints;

    /// <summary>
    /// 图片上的点
    /// </summary>
    MatOfPoint2f imagePoints;

    /// <summary>
    /// The webcam texture to mat helper.
    /// </summary>
    WebCamTextureToMatHelper webCamTextureToMatHelper;

    /// <summary>
    /// The sp_human_face_68_dat_filepath.
    /// </summary>
    string sp_human_face_68_dat_filepath;

    /// <summary>
    /// The cameraparam matrix. 摄像机参数的矩阵
    /// </summary>
    Mat camMatrix;

    /// <summary>
    /// The distortion coeffs. 扭曲多项式系数
    /// </summary>
    MatOfDouble distCoeffs;

    /// <summary>
    /// The matrix that inverts the Y axis. 把Y轴颠倒的矩阵
    /// </summary>
    Matrix4x4 invertYM;

    /// <summary>
    /// The matrix that inverts the Z axis. 使Z轴反转的矩阵
    /// </summary>
    Matrix4x4 invertZM;

    /// <summary>
    /// The rvec.
    /// </summary>
    Mat rvec;

    /// <summary>
    /// The tvec.
    /// </summary>
    Mat tvec;

    Texture2D texture;

    [SerializeField]
    public CubismParameterBlendMode BlendMode = CubismParameterBlendMode.Additive;
    [SerializeField]
    public CubismParameter paramAngleX;
    private CubismParameter ParamAngleX
    {
        get { return paramAngleX; }
        set { paramAngleX = value; }
    }
    [SerializeField]
    private CubismParameter paramAngleY;
    private CubismParameter ParamAngleY
    {
        get { return paramAngleY; }
        set { paramAngleY = value; }
    }
    [SerializeField]
    private CubismParameter paramAngleZ;
    private CubismParameter ParamAngleZ
    {
        get { return paramAngleZ; }
        set { paramAngleZ = value; }
    }
    [SerializeField]
    private CubismParameter paramEyeLOpen;
    private CubismParameter ParamEyeLOpen
    {
        get { return paramEyeLOpen; }
        set { paramEyeLOpen = value; }
    }
    [SerializeField]
    private CubismParameter paramEyeROpen;
    private CubismParameter ParamEyeROpen
    {
        get { return paramEyeROpen; }
        set { paramEyeROpen = value; }
    }
    [SerializeField]
    private CubismParameter paramEyeBallX;
    private CubismParameter ParamEyeBallX
    {
        get { return paramEyeBallX; }
        set { paramEyeBallX = value; }
    }
    [SerializeField]
    private CubismParameter paramEyeBallY;
    private CubismParameter ParamEyeBallY
    {
        get { return paramEyeBallY; }
        set { paramEyeBallY = value; }
    }
    [SerializeField]
    private CubismParameter paramBrowRY;
    private CubismParameter ParamBrowRY
    {
        get { return paramBrowRY; }
        set { paramBrowRY = value; }
    }
    [SerializeField]
    private CubismParameter paramBrowLY;
    private CubismParameter ParamBrowLY
    {
        get { return paramBrowLY; }
        set { paramBrowLY = value; }
    }
    [SerializeField]
    private CubismParameter paramMouthOpenY;
    private CubismParameter ParamMouthOpenY
    {
        get { return paramMouthOpenY; }
        set { paramMouthOpenY = value; }
    }
    [SerializeField]
    private CubismParameter paramMouthForm;
    private CubismParameter ParamMouthForm
    {
        get { return paramMouthForm; }
        set { paramMouthForm = value; }
    }
    [SerializeField]
    private GameObject quad;
    [SerializeField]
    private GameObject text;
    /// <summary>
    /// The rot mat.
    /// </summary>
    Mat rotMat;

    /// <summary>
    /// The transformation matrix for AR.
    /// </summary>
    Matrix4x4 ARM;

    /// <summary>
    /// The transformation matrix.
    /// </summary>
    Matrix4x4 transformationM = new Matrix4x4();

    Dictionary<CubismParameter, float> cubismParameterDictionary = new Dictionary<CubismParameter, float>();

#if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
#endif

    // Use this for initialization
    void Start ()
    {
#if UNITY_WEBGL && !UNITY_EDITO
        var getFilePath_Coroutine = DlibFaceLandmarkDetector.Utils.getFilePathAsync("sp_human_face_68.dat", (result) => {
            coroutines.Clear();

            sp_human_face_68_dat_filepath = result;
            Run();
        });
        coroutines.Push(getFilePath_Coroutine);
        StartCoroutine(getFilePath_Coroutine);
#else
        sp_human_face_68_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath("sp_human_face_68.dat");
          Run();
        #endif
     }

    private void Run()
     {
        //set 3d face object points. 设置3d脸的对象点
        objectPoints = new MatOfPoint3f(
                new Point3(-31, 72, 86),//l eye
                new Point3(31, 72, 86),//r eye
                new Point3(0, 40, 114),//nose
                new Point3(-20, 15, 90),//l mouth //new Point3(-22, 17, 90),//l mouth
                new Point3(20, 15, 90),//r mouth //new Point3(22, 17, 90),//r mouth
                new Point3(-69, 76, -2),//l ear
                new Point3(69, 76, -2)//r ear
        );
        imagePoints = new MatOfPoint2f();
        rotMat = new Mat(3, 3, CvType.CV_64FC1);
        //设置dat
        faceLandmarkDetector = new FaceLandmarkDetector(sp_human_face_68_dat_filepath);
        //获取相机的mat
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        webCamTextureToMatHelper.Initialize();
     }
    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
        {
            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

            // Detect faces on resize image 在调整图像时察觉脸
                    //detect face rects 发现脸的矩形
                    List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();
          ;
            if (detectResult.Count > 0)
             {
                //detect landmark points 检测具有界标意义的点
                List<Vector2> points = faceLandmarkDetector.DetectLandmark(detectResult[0]);
                //将points绘制在mat上
                OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                imagePoints.fromArray(
                            new Point((points[38].x + points[41].x) / 2, (points[38].y + points[41].y) / 2),//l eye (Interpupillary breadth)
                            new Point((points[43].x + points[46].x) / 2, (points[43].y + points[46].y) / 2),//r eye (Interpupillary breadth)
                            new Point(points[30].x, points[30].y),//nose (Nose top)
                            new Point(points[48].x, points[48].y),//l mouth (Mouth breadth)
                            new Point(points[54].x, points[54].y), //r mouth (Mouth breadth)
                            new Point(points[0].x, points[0].y),//l ear (Bitragion breadth)
                            new Point(points[16].x, points[16].y)//r ear (Bitragion breadth)
                        );

                 // Estimate head pose. 估计头部姿势
                 if (rvec == null || tvec == null)
                {
                     rvec = new Mat(3, 1, CvType.CV_64FC1);
                     tvec = new Mat(3, 1, CvType.CV_64FC1);
                     //从3D到2D点的pose中找到一个物体的姿势 rvec是旋转 tvec是平移向量
                     Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                           
                 }

                double tvec_z = tvec.get(2, 0)[0];

                if (double.IsNaN(tvec_z) || tvec_z < 0)
                { 
                    Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                }
                else
                {
                    Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec, true, Calib3d.SOLVEPNP_ITERATIVE);
                }

                if (!double.IsNaN(tvec_z) && points.Count == 68)
                {
                    cubismParameterDictionary.Clear();
                    Calib3d.Rodrigues(rvec, rotMat);

                    transformationM.SetRow(0, new Vector4((float)rotMat.get(0, 0)[0], (float)rotMat.get(0, 1)[0], (float)rotMat.get(0, 2)[0], (float)tvec.get(0, 0)[0]));
                    transformationM.SetRow(1, new Vector4((float)rotMat.get(1, 0)[0], (float)rotMat.get(1, 1)[0], (float)rotMat.get(1, 2)[0], (float)tvec.get(1, 0)[0]));
                    transformationM.SetRow(2, new Vector4((float)rotMat.get(2, 0)[0], (float)rotMat.get(2, 1)[0], (float)rotMat.get(2, 2)[0], (float)tvec.get(2, 0)[0]));
                    transformationM.SetRow(3, new Vector4(0, 0, 0, 1));

                    ARM = invertYM * transformationM * invertZM;
                    Vector3 forward;
                    forward.x = ARM.m02;
                    forward.y = ARM.m12;
                    forward.z = ARM.m22;

                    Vector3 upwards;
                    upwards.x = ARM.m01;
                    upwards.y = ARM.m11;
                    upwards.z = ARM.m21;

                    Vector3 angles = Quaternion.LookRotation(forward, upwards).eulerAngles;
                    float rotateX = angles.x > 180 ? angles.x - 360 : angles.x;
                    cubismParameterDictionary.Add(ParamAngleY, (float)Math.Round(rotateX));
                    float rotateY = angles.y > 180 ? angles.y - 360 : angles.y ;
                    cubismParameterDictionary.Add(ParamAngleX, (float)Math.Round(-rotateY) * 2);
                    float rotateZ = angles.z > 180 ? angles.z - 360 : angles.z;
                    cubismParameterDictionary.Add(ParamAngleZ, (float)Math.Round(-rotateZ) * 2);
                    //Debug.("X" + rotateX + "Y" + rotateY + "Z" + rotateZ);

                    //ParamAngleX.BlendToValue(BlendMode,(float)(Math.Round(-rotateY) * 2));
                    //ParamAngleY.BlendToValue(BlendMode, (float)Math.Round(rotateX));
                    //ParamAngleZ.BlendToValue(BlendMode, (float)Math.Round(-rotateZ) * 2);

                    float eyeOpen_L = Mathf.Clamp(Mathf.Abs(points[43].y - points[47].y) / (Mathf.Abs(points[43].x - points[44].x) * 0.75f), -0.1f, 2.0f);
                   if (eyeOpen_L >= 0.8f)
                        eyeOpen_L = 1f;
                   else 
                        eyeOpen_L = 0;


                    float eyeOpen_R = Mathf.Clamp(Mathf.Abs(points[38].y - points[40].y) / (Mathf.Abs(points[37].x - points[38].x) * 0.75f), -0.1f, 2.0f);
                    if (eyeOpen_R >= 0.8f)
                         eyeOpen_R = 1f;
                    else
                         eyeOpen_R = 0;

                    // ParamEyeROpen.BlendToValue(BlendMode, eyeOpen_R);
                    cubismParameterDictionary.Add(ParamEyeROpen, eyeOpen_R);
                    // ParamEyeLOpen.BlendToValue(BlendMode, eyeOpen_L);
                    cubismParameterDictionary.Add(ParamEyeLOpen, eyeOpen_L);
                    // ParamEyeBallX.BlendToValue(BlendMode, (float)rotateY / 30f);
                    cubismParameterDictionary.Add(ParamEyeBallX, rotateY / 30f);
                    // ParamEyeBallX.BlendToValue(BlendMode, (float)-rotateX / 30f - 0.25f);
                    cubismParameterDictionary.Add(ParamEyeBallY, (float)-rotateX / 30f - 0.25f);

                    float RY = Mathf.Abs(points[19].y - points[27].y) / Mathf.Abs(points[27].y - points[29].y);
                    RY -= 1;
                    RY *= 4f;
                    float LY = Mathf.Abs(points[24].y - points[27].y) / Mathf.Abs(points[27].y - points[29].y);
                    LY -= 1;
                    LY *= 4f;

                    // ParamBrowRY.BlendToValue(BlendMode, RY);
                    cubismParameterDictionary.Add(ParamBrowRY, RY);
                    // ParamBrowLY.BlendToValue(BlendMode, LY);
                    cubismParameterDictionary.Add(ParamBrowLY, LY);
                    float mouthOpen = Mathf.Clamp01(Mathf.Abs(points[62].y - points[66].y) / (Mathf.Abs(points[51].y - points[62].y) + Mathf.Abs(points[66].y - points[57].y)));
                    if (mouthOpen < 0.6f)
                        mouthOpen = 0;
                    // ParamMouthOpenY.BlendToValue(BlendMode, mouthOpen);
                    cubismParameterDictionary.Add(ParamMouthOpenY, mouthOpen);
                    float mouthSize = Mathf.Abs(points[48].x - points[54].x) / (Mathf.Abs(points[31].x - points[35].x));
                    // ParamMouthForm.BlendToValue(BlendMode, Mathf.Clamp(mouthSize, -1.0f, 1.0f));
                    cubismParameterDictionary.Add(ParamMouthForm, Mathf.Clamp(mouthSize, -1.0f, 1.0f));

                }
            }
            OpenCVForUnity.Utils.matToTexture2D(rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors());
        }
    }

    private void LateUpdate()
    {
        foreach(KeyValuePair<CubismParameter,float> kv in cubismParameterDictionary){
            Debug.Log(kv.Key.name + " " + kv.Value);
            kv.Key.BlendToValue(BlendMode, kv.Value);
        }
    }
    /// <summary>
    /// Raises the web cam texture to mat helper initialized event.
    /// </summary>
    public void OnWebCamTextureToMatHelperInitialized()
    {
        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();

        texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
        quad.GetComponent<RawImage>().texture = texture;
        float imageSizeScale = 1.0f;
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            imageSizeScale = (float)Screen.height / (float)Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }


        //set cameraparam 设置相机参数
        int max_d = (int)Mathf.Max(width, height);
        double fx = max_d;
        double fy = max_d;
        double cx = width / 2.0f;
        double cy = height / 2.0f;
        camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);
        //Debug.Log("camMatrix " + camMatrix.dump());


        distCoeffs = new MatOfDouble(0, 0, 0, 0);
        //Debug.Log("distCoeffs " + distCoeffs.dump());


        //calibration camera 校准
        Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
        double apertureWidth = 0;
        double apertureHeight = 0;
        double[] fovx = new double[1];
        double[] fovy = new double[1];
        double[] focalLength = new double[1];
        Point principalPoint = new Point(0, 0);
        double[] aspectratio = new double[1];

        //可以得到摄像机投影透视方程的投影矩阵（摄像机参数的矩阵,图片的大小,孔径宽度, 孔径长度,视场角x,视场角y,焦距,主点,纵横比）
        Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);
        double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
        double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

        /* Debug.Log("imageSize " + imageSize.ToString());
         Debug.Log("apertureWidth " + apertureWidth);
         Debug.Log("apertureHeight " + apertureHeight);
         Debug.Log("fovx " + fovx[0]);
         Debug.Log("fovy " + fovy[0]);
         Debug.Log("focalLength " + focalLength[0]);
         Debug.Log("principalPoint " + principalPoint.ToString());
         Debug.Log("aspectratio " + aspectratio[0]);
         */

        invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
        //Debug.Log("invertYM " + invertYM.ToString());

        invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        //Debug.Log("invertZM " + invertZM.ToString());


    }

    /// <summary>
    /// Raises the web cam texture to mat helper disposed event.
    /// </summary>
    public void OnWebCamTextureToMatHelperDisposed()
    {
        camMatrix.Dispose();
        distCoeffs.Dispose();
    }

    /// <summary>
    /// Raises the web cam texture to mat helper error occurred event.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        if (webCamTextureToMatHelper != null)
            webCamTextureToMatHelper.Dispose();

        if (faceLandmarkDetector != null)
            faceLandmarkDetector.Dispose();

        #if UNITY_WEBGL && !UNITY_EDITOR
                    foreach (var coroutine in coroutines) {
                        StopCoroutine (coroutine);
                        ((IDisposable)coroutine).Dispose ();
                    }
        #endif
    }
}
