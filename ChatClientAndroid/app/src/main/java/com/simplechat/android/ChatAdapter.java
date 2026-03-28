package com.simplechat.android;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.text.SpannableString;
import android.text.method.LinkMovementMethod;
import android.text.util.Linkify;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.fragment.app.FragmentActivity;
import androidx.recyclerview.widget.RecyclerView;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;

public class ChatAdapter extends RecyclerView.Adapter<ChatAdapter.MessageViewHolder> {
    private static final int VIEW_TYPE_SENT = 1;
    private static final int VIEW_TYPE_RECEIVED = 2;
    
    private final List<ChatMessage> messages = new ArrayList<>();
    private final String currentUserName;
    private final SimpleDateFormat dateFormat = new SimpleDateFormat("HH:mm", Locale.getDefault());

    public ChatAdapter(String currentUserName) {
        this.currentUserName = currentUserName;
    }

    @Override
    public int getItemViewType(int position) {
        ChatMessage message = messages.get(position);
        String from = message.getUserFrom();
        return from == null || from.equals(currentUserName) ? VIEW_TYPE_SENT : VIEW_TYPE_RECEIVED;
    }

    @NonNull
    @Override
    public MessageViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        int layoutId = viewType == VIEW_TYPE_SENT ? R.layout.item_message_sent : R.layout.item_message_received;
        View view = LayoutInflater.from(parent.getContext()).inflate(layoutId, parent, false);
        return new MessageViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull MessageViewHolder holder, int position) {
        ChatMessage message = messages.get(position);
        holder.bind(message);
    }

    @Override
    public int getItemCount() {
        return messages.size();
    }

    public void addMessage(ChatMessage message) {
        // Prevent adding duplicate messages by Id
        for (ChatMessage m : messages) {
            if (m.getId().equals(message.getId())) {
                return;
            }
        }
        messages.add(message);
        notifyItemInserted(messages.size() - 1);
    }

    public void clearMessages() {
        messages.clear();
        notifyDataSetChanged();
    }

    class MessageViewHolder extends RecyclerView.ViewHolder {
        private final TextView messageText;
        private final TextView timeText;
        private final TextView userText;
        private final ImageView messageImage;

        public MessageViewHolder(@NonNull View itemView) {
            super(itemView);
            messageText = itemView.findViewById(R.id.messageText);
            timeText = itemView.findViewById(R.id.timeText);
            userText = itemView.findViewById(R.id.userText);
            messageImage = itemView.findViewById(R.id.messageImage);
        }

        public void bind(ChatMessage message) {
            Bitmap bitmap = null;
            switch (message.getContentType()) {
                case ChatMessage.ChatMessageContentType_Text:
                    messageText.setVisibility(View.VISIBLE);
                    messageImage.setVisibility(View.GONE);
                    SpannableString spannable = new SpannableString(message.getMessage() != null ? message.getMessage() : "");
                    Linkify.addLinks(spannable, Linkify.WEB_URLS);
                    messageText.setText(spannable);
                    if (spannable.getSpans(0, spannable.length(), Object.class).length > 0) {
                        messageText.setMovementMethod(LinkMovementMethod.getInstance());
                    } else {
                        messageText.setMovementMethod(null);
                    }
                    break;
                case ChatMessage.ChatMessageContentType_Image:
                    messageText.setVisibility(View.GONE);
                    messageImage.setVisibility(View.VISIBLE);
                    try {
                        byte[] imageBytes = ByteStringConverter.fromZ85String(message.getMessage());
                        bitmap = BitmapFactory.decodeByteArray(imageBytes, 0, imageBytes.length);
                        messageImage.setImageBitmap(bitmap);
                        messageImage.getLayoutParams().width = 500;
                        messageImage.getLayoutParams().height = 500;
                        messageImage.requestLayout();
                    } catch (Exception e) {
                        messageImage.setImageDrawable(null);
                    }
                    break;
                case ChatMessage.ChatMessageContentType_File:
                    messageText.setVisibility(View.VISIBLE);
                    messageImage.setVisibility(View.GONE);
                    messageText.setText("[File: " + message.getContentName() + "]");
                    break;
                case ChatMessage.ChatMessageContentType_Emoji:
                    messageText.setVisibility(View.GONE);
                    messageImage.setVisibility(View.VISIBLE);
                    try {
                        byte[] imageBytes = ByteStringConverter.fromZ85String(message.getMessage());
                        bitmap = BitmapFactory.decodeByteArray(imageBytes, 0, imageBytes.length);
                        messageImage.setImageBitmap(bitmap);
                        messageImage.getLayoutParams().width = 150;
                        messageImage.getLayoutParams().height = 150;
                        messageImage.requestLayout();
                    } catch (Exception e) {
                        messageImage.setImageDrawable(null);
                    }
                    break;
            }
            // Set click listener for zoom if bitmap is available
            if (bitmap != null) {
                Bitmap finalBitmap = bitmap;
                messageImage.setOnClickListener(v -> {
                    FragmentActivity activity = (FragmentActivity) v.getContext();
                    ImageZoomDialogFragment.newInstance(finalBitmap)
                        .show(activity.getSupportFragmentManager(), "zoom");
                });
            } else {
                messageImage.setOnClickListener(null);
            }
            Date date = DateFormatter.parse(message.getTimestamp());
            SimpleDateFormat outputFormat = new SimpleDateFormat("HH:mm", Locale.getDefault());
            timeText.setText(date != null ? outputFormat.format(date) : message.getTimestamp());
            userText.setText(message.getUserFrom());
        }
    }
}
