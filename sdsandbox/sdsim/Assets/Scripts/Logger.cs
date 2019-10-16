﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System;
using UnityEngine.UI;

public class Logger : MonoBehaviour {

	public GameObject carObj;
	public ICar car;
	public CameraSensor camSensor;
    public CameraSensor optionlB_CamSensor;
	public Lidar lidar;

	//what's the current frame index
    public int frameCounter = 0;

    //which lap
    public int lapCounter = 0;

	//is there an upper bound on the number of frames to log
	public int maxFramesToLog = 14000;

	//should we log when we are enabled
	public bool bDoLog = true;

    public int limitFPS = 30;

    float timeSinceLastCapture = 0.0f;

    //The style for xilinx pynq_car using command
    public bool PynqStyle = false;

    public bool PynqStyle2 = true;

    public Text logDisplay;

	string outputFilename = "train.csv";
	private StreamWriter writer;

	class ImageSaveJob {
		public string filename;
		public byte[] bytes;
	}
		
	List<ImageSaveJob> imagesToSave;

	Thread thread;

    string GetLogPath()
    {
        if(GlobalState.log_path != "default")
            return GlobalState.log_path + "/";

        return Application.dataPath + "/../log/";
    }

	void Awake()
	{
		car = carObj.GetComponent<ICar>();

		if(bDoLog && car != null)
		{
			if(PynqStyle || PynqStyle2){
                outputFilename = "train.csv";
            }

			string filename = GetLogPath() + outputFilename;

			writer = new StreamWriter(filename);

			Debug.Log("Opening file for log at: " + filename);

		}

        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        GameObject go = CarSpawner.getChildGameObject(canvas.gameObject, "LogCount");
        if (go != null)
            logDisplay = go.GetComponent<Text>();

        imagesToSave = new List<ImageSaveJob>();

		thread = new Thread(SaverThread);
		thread.Start();
	}
		
	// Update is called once per frame
	void Update () 
	{
		if(!bDoLog)
			return;

        timeSinceLastCapture += Time.deltaTime;

        if (timeSinceLastCapture < 1.0f / limitFPS)
            return;

        timeSinceLastCapture -= (1.0f / limitFPS);

        string activity = car.GetActivity();

		if(writer != null)
		{
			if(PynqStyle){
                string image_filename = GetPynqStyleImageFilename();
                char comNow = car.GetComNow();
                int commandGet = 0;
                switch(comNow){
                    case 'w':
                        commandGet = 1;
                        break;
                    case 's':
                        commandGet = 3;
                        break;
                    case 'a':
                        commandGet = 0;
                        break;
                    case 'd':
                        commandGet = 2;
                        break;
                    default:
                        return;
                }
				writer.WriteLine(string.Format("{0},{1}", image_filename,commandGet.ToString()));
            }else if(PynqStyle2){
                string image_filename = GetPynqStyleImageFilename();
                float st = car.GetSteering();
                float left = 0.0f;
                float straight = 0.0f;
                float right = 0.0f;
                
                if(st < 0){
                    left = 1.0f;
                }
                else if(st > 0){
                    right = 1.0f;
                }else{straight = 1.0f;}
                writer.WriteLine(string.Format("{0},{1},{2},{3}",image_filename,left.ToString(),straight.ToString(),right.ToString()));
            }
		}

		if(lidar != null)
		{
			LidarPointArray pa = lidar.GetOutput();

			if(pa != null)
			{
				string json = JsonUtility.ToJson(pa);
				var filename = string.Format("lidar_{0}_{1}.txt", frameCounter.ToString(), activity);
				var f = File.CreateText(GetLogPath() + filename);
				f.Write(json);
				f.Close();
			}
		}

        if (optionlB_CamSensor != null)
        {
            SaveCamSensor(camSensor, activity, "_a");
            SaveCamSensor(optionlB_CamSensor, activity, "_b");
        }
        else
        {
            SaveCamSensor(camSensor, activity, "");
        }

        if (maxFramesToLog != -1 && frameCounter >= maxFramesToLog)
        {
            Shutdown();
            this.gameObject.SetActive(false);
        }

        frameCounter = frameCounter + 1;

        if (logDisplay != null)
            logDisplay.text = "Log:" + frameCounter;
	}
    
    string GetPynqStyleImageFilename()
	{
		return string.Format("images_{0,8:D8}.jpg", frameCounter);
	}
	
    //Save the camera sensor to an image. Use the suffix to distinguish between cameras.
    void SaveCamSensor(CameraSensor cs, string prefix, string suffix)
    {
        if (cs != null)
        {
            Texture2D image = cs.GetImage();

            ImageSaveJob ij = new ImageSaveJob();
            if(PynqStyle || PynqStyle2){
                ij.filename = GetLogPath()+GetPynqStyleImageFilename();
                ij.bytes = image.EncodeToJPG();
            }
            lock (this)
            {
                imagesToSave.Add(ij);
            }
        }
    }

    public void SaverThread()
	{
		while(true)
		{
			int count = 0;

			lock(this)
			{
				count = imagesToSave.Count; 
			}

			if(count > 0)
			{
				ImageSaveJob ij = imagesToSave[0];

                //Debug.Log("saving: " + ij.filename);

                File.WriteAllBytes(ij.filename, ij.bytes);

				lock(this)
				{
					imagesToSave.RemoveAt(0);
				}
			}
		}
	}

	public void Shutdown()
	{
		if(writer != null)
		{
			writer.Close();
			writer = null;
		}

		if(thread != null)
		{
			thread.Abort();
			thread = null;
		}

		bDoLog = false;
	}

	void OnDestroy()
	{
		Shutdown();
	}
}

