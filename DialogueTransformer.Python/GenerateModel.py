import pandas as pd
from sklearn.model_selection import train_test_split
from simplet5 import SimpleT5

import os

df = pd.read_csv("KhajiitTranslations.csv", usecols=['source_text', 'target_text'])
df['source_text'] = "khajiit: " + df['source_text']
print(df.head())
train_df, test_df = train_test_split(df, test_size=0.3)
model = SimpleT5()
model.from_pretrained(model_type="t5", model_name="t5-base")
model.train(train_df = train_df,
            eval_df = test_df,
            source_max_token_len = 160,
            target_max_token_len = 300,
            batch_size = 8,
            max_epochs = 5,
            outputdir = "outputs",
            dataloader_num_workers = 24,
            use_gpu = True)
