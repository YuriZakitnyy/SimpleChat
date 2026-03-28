package com.simplechat.android;

import android.Manifest;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.graphics.Paint;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;
import android.widget.ImageButton;
import android.widget.TextView;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.google.firebase.messaging.FirebaseMessaging;

import java.util.Date;
import java.util.List;
import java.util.UUID;

public class MainActivity extends Activity
{
    private HubClient hubClient;
    private ChatAdapter chatAdapter;
    private SettingsManager settingsManager;
    private MessageRepository messageRepository;

    private EditText urlInput;
    private EditText userNameInput;
    private EditText messageInput;
    private Button connectButton;
    private Button disconnectButton;
    private Button sendButton;
    private Button sendEmojiButton;
    private ImageButton chooseEmojiButton;
    private RecyclerView messagesRecyclerView;
    private static final int REQUEST_CODE_POST_NOTIFICATIONS = 2001;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        Log.d("MainActivity", "onCreate: instance=" + this);

        // Ask for POST_NOTIFICATIONS permission on startup (Android 13+)
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.TIRAMISU) {
            if (ContextCompat.checkSelfPermission(this, android.Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
                ActivityCompat.requestPermissions(this, new String[]{android.Manifest.permission.POST_NOTIFICATIONS}, REQUEST_CODE_POST_NOTIFICATIONS);
            }
        }

        settingsManager = new SettingsManager(this);
        messageRepository = new MessageRepository(this);

        initializeViews();
        setupListeners();
        loadSettings();
        setupConnectionSettingsCollapsible();
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        Log.d("MainActivity", "onNewIntent: instance=" + this);
        ensureConnected();
    }

    private void initializeViews() {
        urlInput = findViewById(R.id.urlInput);
        userNameInput = findViewById(R.id.userNameInput);
        messageInput = findViewById(R.id.messageInput);
        connectButton = findViewById(R.id.connectButton);
        disconnectButton = findViewById(R.id.disconnectButton);
        sendButton = findViewById(R.id.sendButton);
        messagesRecyclerView = findViewById(R.id.messagesRecyclerView);

        chatAdapter = new ChatAdapter(settingsManager.getUserName());
        messagesRecyclerView.setLayoutManager(new LinearLayoutManager(this));
        messagesRecyclerView.setAdapter(chatAdapter);

        disconnectButton.setEnabled(false);
        sendButton.setEnabled(false);

        chooseEmojiButton  = findViewById(R.id.emojiButton);
        chooseEmojiButton.setImageResource(R.drawable.ic_emoji); // Set initial emoji icon
        sendEmojiButton = findViewById(R.id.sendEmojuButton);
        sendEmojiButton.setEnabled(false);
        chooseEmojiButton.setImageBitmap(emojiToBitmap(Emojis.EMOJIS[0])); // Set default emoji
        chooseEmojiButton.setDrawingCacheEnabled(true);
    }

    private void setupListeners() {
        connectButton.setOnClickListener(v -> connect());
        disconnectButton.setOnClickListener(v -> disconnect());
        sendButton.setOnClickListener(v -> sendMessage());

        sendEmojiButton.setOnClickListener(v -> sendEmoji());
        chooseEmojiButton.setOnClickListener(v -> showEmojiPicker());
        chooseEmojiButton.setImageBitmap(emojiToBitmap(Emojis.EMOJIS[0])); // Set default emoji
        chooseEmojiButton.setDrawingCacheEnabled(true);
    }

    @SuppressLint("CheckResult")
    private void loadMessages()
    {
        chatAdapter.clearMessages();
        messageRepository.loadMessages();
        ChatMessage last = null;
        for (ChatMessage message : messageRepository.getMessages()) {
            last = message;
            chatAdapter.addMessage(message);
        }
        messagesRecyclerView.scrollToPosition(chatAdapter.getItemCount() - 1);
        hubClient.listMessages(last != null ? last.getId() : null, settingsManager.getUserName())
                .subscribe(messages -> runOnUiThread(() -> {
                    if (messages != null)
                    {
                        Log.d("MainActivity", "listMessages got messages: " + messages.size());
                        for (ChatMessage message : messages)
                        {
                            chatAdapter.addMessage(message);
                            messageRepository.addMessage(message);
                        }
                        messagesRecyclerView.scrollToPosition(chatAdapter.getItemCount() - 1);
                    }
                }), error -> runOnUiThread(() ->
                    {
                        Log.e("MainActivity", "listMessages failed", error);
                    }));
    }

    private void loadSettings() {
        urlInput.setText(settingsManager.getBackendUrl());
        userNameInput.setText(settingsManager.getUserName());
    }

    private void connect() {
        String url = urlInput.getText().toString().trim();
        String userName = userNameInput.getText().toString().trim();

        if (url.isEmpty() || userName.isEmpty()) {
            Toast.makeText(this, "Please enter URL and username", Toast.LENGTH_SHORT).show();
            return;
        }

        settingsManager.saveBackendUrl(url);
        settingsManager.saveUserName(userName);
        chatAdapter = new ChatAdapter(userName);
        messagesRecyclerView.setAdapter(chatAdapter);

        connectButton.setEnabled(false);

        doConnect(url);
    }

    private void doConnect(String url)
    {
        hubClient = new HubClient();
        hubClient.setMessageReceivedListener(message -> {
        runOnUiThread(() -> {
            chatAdapter.addMessage(message);
            messagesRecyclerView.scrollToPosition(chatAdapter.getItemCount() - 1);
            messageRepository.addMessage(message);
            });
        });

        hubClient.setConnectionStateListener(new HubClient.ConnectionStateListener() {
            @Override
            public void onConnected() {
                runOnUiThread(() -> {
                connectButton.setEnabled(false);
                disconnectButton.setEnabled(true);
                sendButton.setEnabled(true);
                sendEmojiButton.setEnabled(true);
                register();
                collapseSettings();
                });
            }

            @Override
            public void onDisconnected() {
                runOnUiThread(() -> {
                connectButton.setEnabled(true);
                disconnectButton.setEnabled(false);
                sendButton.setEnabled(false);
                sendEmojiButton.setEnabled(false);
                expandSettings();
                Toast.makeText(MainActivity.this, "Disconnected", Toast.LENGTH_SHORT).show();
                });
            }

            @Override
            public void onReconnecting() {
                runOnUiThread(() ->
                    {
                    expandSettings();
                    });
            }

            @Override
            public void onReconnected(){
                runOnUiThread(() ->
                    {
                    collapseSettings();
                    loadMessages();
                    });
            }
        });

        hubClient.connect(url)
                .subscribe(() -> {
                runOnUiThread(() -> {
                });
                }, error -> {
                runOnUiThread(() ->
                    {
                        connectButton.setEnabled(true);
                        Toast.makeText(MainActivity.this, "Connection failed: " + error.getMessage(), Toast.LENGTH_LONG).show();
                    });
                });
    }

    private void register()
    {
        String id = Settings.Secure.getString(SimpleChatApplication.Instance.getContentResolver(), Settings.Secure.ANDROID_ID);
        hubClient.register(id, SimpleChatApplication.Instance.PushNotificationToken)
                .subscribe((val) -> {
                    runOnUiThread(() -> {
                        loadMessages();
                    });
                    }, error -> {
                    runOnUiThread(() ->
                        {
                        loadMessages();
                        });
                    });
    }
    private void disconnect() {
        hubClient.disconnect()
                .subscribe(() -> runOnUiThread(() -> {
                    connectButton.setEnabled(true);
                    disconnectButton.setEnabled(false);
                    sendButton.setEnabled(false);
                    sendEmojiButton.setEnabled(false);
                    expandSettings();
                }), error -> runOnUiThread(() -> Toast.makeText(MainActivity.this, "Disconnection failed: " + error.getMessage(), Toast.LENGTH_LONG).show()));
    }

    private void sendMessage() {
        String messageText = messageInput.getText().toString().trim();
        if (messageText.isEmpty()) {
            return;
        }

        ChatMessage message = new ChatMessage();
        sendButton.setEnabled(false);
        message.setUserFrom(settingsManager.getUserName());
        message.setContentType(ChatMessage.ChatMessageContentType_Text);
        message.setMessage(messageText);

        hubClient.sendMessage(message)
                .subscribe(
                        val -> runOnUiThread(() -> {
                            messageInput.setText("");
                            sendButton.setEnabled(true);
                        }),
                        error -> runOnUiThread(() -> {
                            messageInput.setText("");
                            sendButton.setEnabled(true);
                        })
                );
    }

    private void setupConnectionSettingsCollapsible() {

        final ImageButton expandCollapseButton = findViewById(R.id.expandCollapseButton);
        final TextView connectionSettingsToggle = findViewById(R.id.connectionSettingsToggle);

        expandCollapseButton.setOnClickListener(v -> {
            toggCollapse();
        });
        connectionSettingsToggle.setOnClickListener(v -> {
            toggCollapse();
        });
    }

    private void toggCollapse()
    {
        final View connectionSettingsInclude = findViewById(R.id.connectionSettingsInclude);
        final ImageButton expandCollapseButton = findViewById(R.id.expandCollapseButton);

        if (connectionSettingsInclude.getVisibility() == View.VISIBLE) {
            collapseSettings();
        } else {
            expandSettings();
        }
    }

    private void collapseSettings()
    {
        final View connectionSettingsInclude = findViewById(R.id.connectionSettingsInclude);
        final ImageButton expandCollapseButton = findViewById(R.id.expandCollapseButton);
        connectionSettingsInclude.setVisibility(View.GONE);
        expandCollapseButton.setImageResource(android.R.drawable.arrow_up_float);
    }

    private void expandSettings()
    {
        final View connectionSettingsInclude = findViewById(R.id.connectionSettingsInclude);
        final ImageButton expandCollapseButton = findViewById(R.id.expandCollapseButton);
        connectionSettingsInclude.setVisibility(View.VISIBLE);
        expandCollapseButton.setImageResource(android.R.drawable.arrow_down_float);
    }

    private void sendEmoji()
    {
        // Get image from chooseEmojiButton's drawable
        Bitmap emojiBitmap = null;
        if (chooseEmojiButton.getDrawable() instanceof android.graphics.drawable.BitmapDrawable) {
            emojiBitmap = ((android.graphics.drawable.BitmapDrawable) chooseEmojiButton.getDrawable()).getBitmap();
        }
        if (emojiBitmap == null) {
            // fallback: use default emoji
            emojiBitmap = emojiToBitmap(Emojis.EMOJIS[0]);
        }
        // Convert bitmap to PNG byte array
        java.io.ByteArrayOutputStream baos = new java.io.ByteArrayOutputStream();
        emojiBitmap.compress(Bitmap.CompressFormat.PNG, 100, baos);
        byte[] imageBytes = baos.toByteArray();
        // Convert to Z85 string
        String z85String = ByteStringConverter.toZ85String(imageBytes);
        // Send as message
        ChatMessage emojiMessage = new ChatMessage();
        emojiMessage.setUserFrom(settingsManager.getUserName());
        emojiMessage.setContentType(ChatMessage.ChatMessageContentType_Emoji);
        emojiMessage.setMessage(z85String);
        sendEmojiButton.setEnabled(false);
        hubClient.sendMessage(emojiMessage)
                .subscribe(
                        val -> runOnUiThread(() ->
                            {
                                sendEmojiButton.setEnabled(true);
                            }),
                        error -> runOnUiThread(() ->
                            {
                                sendEmojiButton.setEnabled(true);
                            })
                );
    }
    private void showEmojiPicker() {
        android.view.LayoutInflater inflater = android.view.LayoutInflater.from(this);
        android.view.View dialogView = inflater.inflate(R.layout.emoji_picker_grid, null);
        android.widget.GridView gridView = dialogView.findViewById(R.id.emojiGridView);
        EmojiGridAdapter adapter = new EmojiGridAdapter(this, Emojis.EMOJIS);
        gridView.setAdapter(adapter);

        android.app.AlertDialog dialog = new android.app.AlertDialog.Builder(this)
                .setTitle("Choose an emoji")
                .setView(dialogView)
                .create();

        gridView.setOnItemClickListener((parent, view, position, id) -> {
            String selectedEmoji = Emojis.EMOJIS[position];
            chooseEmojiButton.setImageBitmap(emojiToBitmap(selectedEmoji));
            dialog.dismiss();
        });

        dialog.show();
    }

    private Bitmap emojiToBitmap(String emoji) {
        // Create a bitmap from the emoji string
        android.graphics.Paint paint = new android.graphics.Paint(Paint.ANTI_ALIAS_FLAG);
        paint.setTextSize(64f); // Adjust size as needed
        paint.setTextAlign(android.graphics.Paint.Align.LEFT);
        paint.setColor(android.graphics.Color.BLACK);
        android.graphics.Rect bounds = new android.graphics.Rect();
        paint.getTextBounds(emoji, 0, emoji.length(), bounds);
        Bitmap bitmap = Bitmap.createBitmap(bounds.width() + 32, bounds.height() + 32, Bitmap.Config.ARGB_8888);
        android.graphics.Canvas canvas = new android.graphics.Canvas(bitmap);
        canvas.drawText(emoji, 16, bounds.height() + 16, paint);
        return bitmap;
    }

        @Override
    protected void onDestroy() {
        super.onDestroy();
        hubClient.disconnect();
        messageRepository.saveMessages();
    }

    private void ensureConnected() {
        if (hubClient != null && !hubClient.isConnected())
        {
            String url = settingsManager.getBackendUrl();
            if (url != null && !url.isEmpty())
            {
                doConnect(url);
            }
        }
    }

    @Override
    protected void onResume()
    {
        super.onResume();
        ensureConnected();
    }
}
