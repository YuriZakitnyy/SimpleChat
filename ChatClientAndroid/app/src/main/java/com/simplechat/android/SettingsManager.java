package com.simplechat.android;

import android.content.Context;
import android.content.SharedPreferences;

public class SettingsManager {
    private static final String PREFS_NAME = "ChatSettings";
    private static final String KEY_BACKEND_URL = "BackendUrl";
    private static final String KEY_USERNAME = "UserName";
    
    private final SharedPreferences prefs;

    public SettingsManager(Context context) {
        prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
    }

    public void saveBackendUrl(String url) {
        prefs.edit().putString(KEY_BACKEND_URL, url).apply();
    }

    public String getBackendUrl() {
        return prefs.getString(KEY_BACKEND_URL, "https://chatserver-1-0-0.onrender.com/chatHub");
    }

    public void saveUserName(String userName) {
        prefs.edit().putString(KEY_USERNAME, userName).apply();
    }

    public String getUserName() {
        return prefs.getString(KEY_USERNAME, "I");
    }
}
