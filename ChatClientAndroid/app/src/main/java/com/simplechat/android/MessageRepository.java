package com.simplechat.android;

import android.content.Context;
import android.util.Base64;
import com.google.gson.Gson;
import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileReader;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

public class MessageRepository {
    private static final String FILE_NAME = "messages.jsonl";
    private final File messageFile;
    private final Gson gson;
    private List<ChatMessage> messages;
    private final Context context;

    public MessageRepository(Context context) {
        this.context = context.getApplicationContext();
        this.messageFile = new File(this.context.getFilesDir(), FILE_NAME);
        this.gson = new Gson();
        this.messages = new ArrayList<>();
        loadMessages();
    }

    public void addMessage(ChatMessage message) {
        // Prevent adding duplicate messages by Id
        for (ChatMessage m : messages) {
            if (m.getId().equals(message.getId())) {
                return;
            }
        }
        messages.add(message);
        appendMessageToFile(message);
    }

    public List<ChatMessage> getMessages() {
        return new ArrayList<>(messages);
    }

    private void appendMessageToFile(ChatMessage message) {
        try (FileOutputStream fos = context.openFileOutput(FILE_NAME, Context.MODE_APPEND);
             OutputStreamWriter osw = new OutputStreamWriter(fos);
             BufferedWriter writer = new BufferedWriter(osw)) {
            String json = gson.toJson(message);
            String base64 = Base64.encodeToString(json.getBytes(StandardCharsets.UTF_8), Base64.NO_WRAP);
            writer.write(base64);
            writer.newLine();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void saveMessages() {
        // Overwrite the file with all messages
        try (FileOutputStream fos = context.openFileOutput(FILE_NAME, Context.MODE_PRIVATE);
             OutputStreamWriter osw = new OutputStreamWriter(fos);
             BufferedWriter writer = new BufferedWriter(osw)) {
            for (ChatMessage message : messages) {
                String json = gson.toJson(message);
                String base64 = Base64.encodeToString(json.getBytes(StandardCharsets.UTF_8), Base64.NO_WRAP);
                writer.write(base64);
                writer.newLine();
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void loadMessages() {
        messages = new ArrayList<>();
        if (!messageFile.exists()) {
            return;
        }
        try (BufferedReader reader = new BufferedReader(new FileReader(messageFile))) {
            String line;
            while ((line = reader.readLine()) != null) {
                try {
                    String json = new String(Base64.decode(line, Base64.NO_WRAP), StandardCharsets.UTF_8);
                    ChatMessage message = gson.fromJson(json, ChatMessage.class);
                    if (message != null) {
                        messages.add(message);
                    }
                } catch (Exception e) {
                    e.printStackTrace(); // log and skip malformed lines
                }
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void clearMessages() {
        messages.clear();
        // Overwrite the file with nothing
        try (FileOutputStream fos = context.openFileOutput(FILE_NAME, Context.MODE_PRIVATE)) {
            // Just open and close to clear
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}
