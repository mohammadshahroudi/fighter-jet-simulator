using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour {

    static ThreadedDataRequester instance;
    readonly Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    void Awake() {
        instance = this;
    }

    public static void RequestData(Func<object> generateData, Action<object> callback) {
        ThreadStart threadStart = delegate {
            instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    void DataThread(Func<object> generateData, Action<object> callback) {
        object data = generateData();

        lock (dataQueue) {
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    void Update() {
        while (true) {
            ThreadInfo threadInfo;

            lock (dataQueue) {
                if (dataQueue.Count == 0) {
                    break;
                }

                threadInfo = dataQueue.Dequeue();
            }

            threadInfo.callback(threadInfo.parameter);
        }
    }

    struct ThreadInfo {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}