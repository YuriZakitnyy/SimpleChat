package com.simplechat.android;

import android.Manifest;
import android.app.Application;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Build;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.core.app.ActivityCompat;
import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

import com.google.android.gms.tasks.OnFailureListener;
import com.google.firebase.FirebaseApp;
import com.google.firebase.messaging.FirebaseMessaging;
import com.google.firebase.messaging.RemoteMessage;

public class SimpleChatApplication extends Application {
    private static final int NOTIFICATION_ID = 1001;
    private static final String DefaultNotificationChannel = "default_channel";
    public static SimpleChatApplication Instance;
    public String PushNotificationToken;
    private NotificationChannel notificationChannel;

    @Override
    public void onCreate() {
        Instance = this;
        super.onCreate();
        subscribeNotifications();
    }

    private void subscribeNotifications()
    {
        FirebaseApp.initializeApp(this);
        FirebaseMessaging.getInstance().getToken().addOnCompleteListener(task ->
            {
            if (task.isSuccessful())
            {
                PushNotificationToken = task.getResult();
                Log.i("FCM_TOKEN", "Push notification key: " +  PushNotificationToken);
            }
            else
            {
                Log.e("FCM_TOKEN", "Failed to get FCM token", task.getException());
            }
            });
    }

    private void createNotificationChannel()
    {
        if ((notificationChannel == null) && (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O))
        {
            CharSequence name = "SimpleChat";
            String description = "SimpleChatDescr";
            int importance = NotificationManager.IMPORTANCE_HIGH;
            notificationChannel = new NotificationChannel(DefaultNotificationChannel, name, importance);
            notificationChannel.setDescription(description);
            NotificationManager notificationManager = getSystemService(NotificationManager.class);
            notificationManager.createNotificationChannel(notificationChannel);
        }
    }

    private void showNotification(String title, String body) {
        createNotificationChannel();

        NotificationManagerCompat notificationManager = NotificationManagerCompat.from(SimpleChatApplication.Instance);

        // Cancel previous notification with the same ID
        notificationManager.cancel(NOTIFICATION_ID);

        // Create intent to open MainActivity
        Intent intent = new Intent(SimpleChatApplication.Instance, MainActivity.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP |
                Intent.FLAG_ACTIVITY_SINGLE_TOP |
                Intent.FLAG_ACTIVITY_NEW_TASK);
        PendingIntent pendingIntent = PendingIntent.getActivity(
                SimpleChatApplication.Instance,
                0,
                intent,
                PendingIntent.FLAG_UPDATE_CURRENT | (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.M ?
                        PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT)
        );
        NotificationCompat.Builder builder = new NotificationCompat.Builder(SimpleChatApplication.Instance, DefaultNotificationChannel)
                .setSmallIcon(R.drawable.ic_emoji)
                .setContentTitle(title)
                .setContentText(body)
                .setPriority(NotificationCompat.PRIORITY_LOW)
                .setContentIntent(pendingIntent)
                .setAutoCancel(true);
        if (ActivityCompat.checkSelfPermission(SimpleChatApplication.Instance, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED)
        {
            return;
        }
        notificationManager.notify(NOTIFICATION_ID, builder.build());
    }

    private boolean isAppInBackground() {
        android.app.ActivityManager activityManager = (android.app.ActivityManager) getSystemService(Context.ACTIVITY_SERVICE);
        java.util.List<android.app.ActivityManager.RunningAppProcessInfo> appProcesses = activityManager.getRunningAppProcesses();
        if (appProcesses == null) return true;
        final String packageName = getPackageName();
        for (android.app.ActivityManager.RunningAppProcessInfo appProcess : appProcesses) {
            if (appProcess.importance == android.app.ActivityManager.RunningAppProcessInfo.IMPORTANCE_FOREGROUND && appProcess.processName.equals(packageName)) {
                return false;
            }
        }
        return true;
    }

    public void onMessageReceived(RemoteMessage remoteMessage) {
        if (isAppInBackground())
        {
            String title = remoteMessage.getNotification() != null ? remoteMessage.getNotification().getTitle() : "SimpleChat";
            String body = remoteMessage.getNotification() != null ? remoteMessage.getNotification().getBody() : "You have a new message";
            showNotification(title, body);
        }
    }
}
