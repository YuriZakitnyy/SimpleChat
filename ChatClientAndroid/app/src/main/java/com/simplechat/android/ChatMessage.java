package com.simplechat.android;

import com.google.gson.annotations.SerializedName;
import java.util.UUID;

public class ChatMessage {
    public static final int ChatMessageContentType_Text = 0;
    public static final int ChatMessageContentType_Image = 1;
    public static final int ChatMessageContentType_File = 2;
    public static final int ChatMessageContentType_Emoji = 3;

    //@SerializedName("Id")
    private String id;
    //@SerializedName("Timestamp")
    private String timestamp;
    //@SerializedName("UserFrom")
    private String userFrom;
    //@SerializedName("UserTo")
    private String userTo;
    //@SerializedName("ContentType")
    private int contentType;
    //@SerializedName("Message")
    private String message;
    //@SerializedName("ContentName")
    private String contentName;

    public ChatMessage() {
        this.id = UUID.randomUUID().toString();
        this.timestamp = DateFormatter.formatNowUtc();
    }

    // Getters and Setters
    public String getId() { return id; }
    public void setId(String id) { this.id = id; }

    public String getUserFrom() { return userFrom; }
    public void setUserFrom(String userFrom) { this.userFrom = userFrom; }

    public String getUserTo() { return userTo; }
    public void setUserTo(String userTo) { this.userTo = userTo; }

    public int getContentType() { return contentType; }
    public void setContentType(int contentType) { this.contentType = contentType; }

    public String getMessage() { return message; }
    public void setMessage(String message) { this.message = message; }

    public String getContentName() { return contentName; }
    public void setContentName(String contentName) { this.contentName = contentName; }

    public String getTimestamp() { return timestamp; }
    public void setTimestamp(String timestamp) { this.timestamp = timestamp; }

    public String getMessageDescription()
    {
        switch (contentType)
        {
            case ChatMessageContentType_Text:
                return message;
            case ChatMessageContentType_Image:
                return "[Image]";
            case ChatMessageContentType_File:
                return "[File: " + contentName + "]";
            case ChatMessageContentType_Emoji:
                return "[Emoji]";
            default:
                return "[Unknown Content]";
        }
    }
}