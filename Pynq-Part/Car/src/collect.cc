/*
 * @Author: Sauron Wu
 * @GitHub: wutianze
 * @Email: 1369130123qq@gmail.com
 * @Date: 2019-09-20 14:23:08
 * @LastEditors: Sauron Wu
 * @LastEditTime: 2019-10-22 11:34:28
 * @Description: 
 */
#include <assert.h>
#include <sys/stat.h>
#include <unistd.h>
#include <cassert>
#include <chrono>
#include <iostream>
#include <string>
#include <vector>
#include <opencv2/opencv.hpp>
#include <sys/types.h>
#include <sys/stat.h>
#include <ctime>
#include <fstream>
#include <sstream>
#include <thread>
#include "safe_queue.h"
#include <mutex>
#include "control.h"
using namespace cv;
using namespace std;
using namespace std::chrono;

//int threadnum;
mutex mtxQueueStore;
safe_queue<pair<string,Mat>> queueStore;
#define NOTSTART 0
#define STARTRECORD 1
#define EXIT 2

int imgMax = 50000;
int startRecord = NOTSTART;
//mutex mutexshow;
string path = "./images/";

void storeImage()
{
    ofstream outFile;
    outFile.open(path + "train.csv", ios::out | ios::app);
    int count = 0;
    while(count <= imgMax){
	sleep(0.1);
	mtxQueueStore.lock();
        if(startRecord == EXIT){
            mtxQueueStore.unlock();
            break;
        }else if(startRecord == NOTSTART){
            mtxQueueStore.unlock();
            continue;
        }
        mtxQueueStore.unlock();
        pair<string,Mat>tmpQ;
        queueStore.wait_and_pop(tmpQ);
        clock_t now = clock();
        string fileName = to_string(now) + ".jpg";
   	imwrite(path + fileName, tmpQ.second);
        outFile << fileName << ','<< tmpQ.first <<endl;
        count++;
            }
    outFile.close();
}

int main(int argc, char **argv)
{
    if (argc != 4) {
          cout << "Usage of this exe: ./collect 50000(img number to collect) 0.23(run speed) 0/1(1 means see the image)"
             << endl;
        return -1;
      }
    // nn means just use ml, cv means use ml & cv.
    imgMax = atoi(argv[1]);
    float runSpeed = atof(argv[2]);
    int seeImg = atoi(argv[3]);
    PYNQZ2 controller = PYNQZ2();

    VideoCapture cap(0);
    cap.set(CV_CAP_PROP_FRAME_WIDTH, 160);
    cap.set(CV_CAP_PROP_FRAME_HEIGHT, 120);
    Mat image;
    if (access(path.c_str(), 0) == -1)
    {
        mkdir(path.c_str(), 0777);
    }
    bool firstStart = true;
    if(seeImg == 0)namedWindow("just get key");
    thread storeThread = thread(storeImage);
    while (startRecord != EXIT)
    {
        cap >> image;
	if(seeImg==1)imshow("car see", image);
        char c = (char)waitKey(1);
        //cout << c << endl;
        switch (c)
        {
        case 'w':
            controller.steerSet(0);
	    controller.throttleSet(runSpeed);
            controller.setLeds(1);
	    if(firstStart){
	    	controller.steerSet(0);
		controller.throttleSet(0.37);
		controller.setLeds(15);
		firstStart=false;
	    }
            break;
        case 'a':
            controller.steerSet(-1.0);
            controller.throttleSet(runSpeed);
            controller.setLeds(2);
            break;
        case 's':
            controller.steerSet(0);
            controller.throttleSet(0);
            controller.setLeds(4);
	    firstStart = true;
            break;
        case 'd':
            controller.steerSet(1.0); 
            controller.throttleSet(runSpeed);
            controller.setLeds(8);
            break;
	    case 't':
            mtxQueueStore.lock();
	        startRecord = STARTRECORD;
            mtxQueueStore.unlock();
	        cout<<"Start Record\n";
	        break;
        case 27:
            mtxQueueStore.lock();
            startRecord = EXIT;
            mtxQueueStore.unlock();
        }
        string oneRecord = controller.to_record();
        queueStore.push(make_pair(oneRecord,image));
    }
    storeThread.join();
    destroyAllWindows();
    cap.release();
    return 0;
}
