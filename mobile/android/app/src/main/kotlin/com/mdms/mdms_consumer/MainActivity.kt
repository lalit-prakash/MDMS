package com.mdms.mdms_consumer

import io.flutter.embedding.android.FlutterFragmentActivity

// FlutterFragmentActivity (not FlutterActivity) is required by the local_auth plugin's
// biometric prompt, which needs a FragmentActivity host.
class MainActivity : FlutterFragmentActivity()
