package com.simplechat.android;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

public class DateFormatter {
    private static final SimpleDateFormat sdf;
    static {
        sdf = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US);
        sdf.setTimeZone(TimeZone.getTimeZone("UTC"));
    }

    public static String formatNowUtc() {
        return sdf.format(new Date());
    }

    public static String format(Date date) {
        return sdf.format(date);
    }

    public static Date parse(String dateString) {
        try {
            return sdf.parse(dateString);
        } catch (Exception e) {
            return null;
        }
    }
}
