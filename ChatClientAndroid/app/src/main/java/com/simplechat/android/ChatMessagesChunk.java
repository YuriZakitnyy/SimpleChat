package com.simplechat.android;

import java.util.List;

public class ChatMessagesChunk
{
    private List<ChatMessage> messages;
    private String next;

    public List<ChatMessage> getMessages() { return messages; }

    public String getNext() { return next; }
}
