package com.simplechat.android;

import android.app.Dialog;
import android.graphics.Bitmap;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.DialogFragment;
import com.davemorrissey.labs.subscaleview.SubsamplingScaleImageView;
import com.davemorrissey.labs.subscaleview.ImageSource;

public class ImageZoomDialogFragment extends DialogFragment {
    private Bitmap imageBitmap;

    public static ImageZoomDialogFragment newInstance(Bitmap bitmap) {
        ImageZoomDialogFragment fragment = new ImageZoomDialogFragment();
        fragment.imageBitmap = bitmap;
        return fragment;
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.dialog_image_zoom, container, false);
        SubsamplingScaleImageView imageView = view.findViewById(R.id.zoomImageView);
        imageView.setImage(ImageSource.bitmap(imageBitmap));
        imageView.setOnClickListener(v -> dismiss());
        return view;
    }

    @Override
    public void onStart() {
        super.onStart();
        Dialog dialog = getDialog();
        if (dialog != null && dialog.getWindow() != null) {
            dialog.getWindow().setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        }
    }
}
