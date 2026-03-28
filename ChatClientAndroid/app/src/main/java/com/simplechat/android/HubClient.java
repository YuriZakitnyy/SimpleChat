package com.simplechat.android;

import android.util.Log;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.reflect.TypeToken;
import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;
import com.microsoft.signalr.HubConnectionState;

import java.lang.reflect.Type;
import java.text.SimpleDateFormat;
import java.util.Collections;
import java.util.Date;
import java.util.List;
import java.util.TimeZone;
import java.util.concurrent.CompletableFuture;

import io.reactivex.rxjava3.core.Completable;
import io.reactivex.rxjava3.core.Single;

public class HubClient {
    private static final String TAG = "HubClient";
    private HubConnection connection;
    private MessageReceivedListener messageReceivedListener;
    private ConnectionStateListener connectionStateListener;
    private Gson gson = new Gson();

    public interface MessageReceivedListener {
        void onMessageReceived(ChatMessage message);
    }

    public interface ConnectionStateListener {
        void onConnected();
        void onDisconnected();
        void onReconnecting();
        void onReconnected();
    }
    public HubClient() {
        // Create Gson with custom date format matching .NET
        gson = new GsonBuilder()
                .setDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'")
                .create();
    }
    public void setMessageReceivedListener(MessageReceivedListener listener) {
        this.messageReceivedListener = listener;
    }

    public void setConnectionStateListener(ConnectionStateListener listener) {
        this.connectionStateListener = listener;
    }

    public Completable connect(String hubUrl) {
        if (connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED) {
            return  Completable.fromFuture(CompletableFuture.completedFuture(null));
        }

        connection = HubConnectionBuilder.create(hubUrl)
                //.re()
                .build();

        connection.on("ReceiveMessage", (message) -> {
            Log.d(TAG, "Message received: " + message);
            if (messageReceivedListener != null) {
                messageReceivedListener.onMessageReceived(message);
            }
        }, ChatMessage.class);

        connection.onClosed(error -> {
            Log.d(TAG, "Connection closed");
            if (connectionStateListener != null) {
                connectionStateListener.onDisconnected();
            }
        });

        // Call onConnected after successful connection
        return connection.start()
                .doOnComplete(() -> {
                    Log.d(TAG, "Connected to hub");
                    if (connectionStateListener != null) {
                        connectionStateListener.onConnected();
                    }
                });
    }

    public Completable disconnect() {
        if (connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED) {
            return connection.stop();
        }
        return Completable.fromFuture(CompletableFuture.completedFuture(null));
    }

    public Single<Boolean> sendMessage(ChatMessage message) {
        if (connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED) {
            return connection.invoke(Boolean.class, "SendMessage2", message);
        }
        return Single.just(false);
    }

    public Single<List<ChatMessage>> listMessages(String last, String userName) {
        if (connection == null || connection.getConnectionState() != HubConnectionState.CONNECTED) {
            return Single.just(Collections.emptyList());
        }

        // Fetch first chunk
        return listMessagesChunkNext(last, userName)
                .flatMap(firstChunk -> {
                List<ChatMessage> allMessages = new java.util.ArrayList<>(firstChunk.getMessages());

                // If there's a next token, recursively fetch remaining chunks
                if (firstChunk.getNext() != null && !firstChunk.getNext().isEmpty()) {
                    return fetchChunksRecursively(firstChunk.getNext(), userName, allMessages);
                } else {
                    return Single.just(allMessages);
                }
                });
    }

    private Single<List<ChatMessage>> fetchChunksRecursively(String nextToken, String userName, List<ChatMessage> accumulator) {
        return listMessagesChunkNext(nextToken, userName)
            .flatMap(chunk -> {
                accumulator.addAll(chunk.getMessages());

                // If there's another chunk, continue fetching
                if (chunk.getNext() != null && !chunk.getNext().isEmpty()) {
                    return fetchChunksRecursively(chunk.getNext(), userName, accumulator);
                } else {
                    return Single.just(accumulator);
                }
            });
    }

    public Single<ChatMessagesChunk> listMessagesChunkNext(String next, String userName) {
        if (connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED) {
            Type type = new TypeToken<ChatMessagesChunk>(){}.getType();
            return connection.invoke(type, "ListMessagesNext", next, userName);
        }
        return Single.just(new ChatMessagesChunk());
    }

    public Single<Boolean> register(String deviceId, String token)
    {
        if (connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED) {
            return connection.invoke(Boolean.class, "RegisterDeviceToken", deviceId, token, "android");
        }
        return Single.just(false);
    }

    public Single<String> ping() {
        if (connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED) {
            return connection.invoke(String.class, "Ping");
        }
        return Single.just("Not connected");
    }

    public boolean isConnected() {
        return connection != null && connection.getConnectionState() == HubConnectionState.CONNECTED;
    }
}
