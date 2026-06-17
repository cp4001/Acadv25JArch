valset : dialog {
    label =  "Valve Type";
    : column {
        : row {
            : column {
               :boxed_radio_row{
                    label =  "Diaphram";
                    : radio_button {
                       label = "Type 1";
                       key   = "zdia1";
                       value = "1";
                    }
                    : radio_button {
                       label = "Type 2";
                       key   = "zdia2";
                       value = "2";
                    }
                }
                : image {
                    key          = "image1";
                    width        = 15;
                    aspect_ratio = 1;
                    color        = 0;
                    allow_accept = true;
                }
            }
            : column {
                :boxed_radio_row{
                    label =  "Buterfly";
                    : radio_button {
                       label = "Type 1";
                       key   = "zbut1";
                       value = "1";
                    }
                    : radio_button {
                       label = "Type 2";
                       key   = "zbut2";
                       value = "2";
                    }
                }
                : image {
                    key          = "image2";
                    width        = 15;
                    aspect_ratio = 1;
                    color        = 0;
                    allow_accept = true;
                }
            }
        }
    }
  ok_cancel;
}
