package com.simplechat.android;

import android.content.Context;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.TextView;

public class EmojiGridAdapter extends BaseAdapter {
    private final String[] emojis;
    private final Context context;

    public EmojiGridAdapter(Context context, String[] emojis) {
        this.context = context;
        this.emojis = emojis;
    }

    @Override
    public int getCount() {
        return emojis.length;
    }

    @Override
    public Object getItem(int position) {
        return emojis[position];
    }

    @Override
    public long getItemId(int position) {
        return position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        TextView textView;
        if (convertView == null) {
            textView = new TextView(context);
            textView.setTextSize(32);
            textView.setPadding(16, 16, 16, 16);
            textView.setGravity(android.view.Gravity.CENTER);
        } else {
            textView = (TextView) convertView;
        }
        textView.setText(emojis[position]);
        return textView;
    }
}

