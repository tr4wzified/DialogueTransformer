from simplet5 import SimpleT5
import argparse
import time
import torch
from time import localtime, strftime
import pandas as pd

parser = argparse.ArgumentParser(
      prog='DialoguePrecacheGenerator',
      description='Generate precache for dialogue using large language models generated with SimpleT5'
)
parser.add_argument('model_path', help="The path to the large language model")
parser.add_argument('csv_path', help='The path to the DialogueTransformations csv that should be predicted')
parser.add_argument('prefix', help="The prefix before the input to the large language model")

args = parser.parse_args()

df = pd.read_csv(args.csv_path)

use_gpu = False
if torch.cuda.is_available():
    use_gpu = True

print(use_gpu)

model = SimpleT5()
model.load_model("t5", args.model_path, use_gpu=use_gpu)

def predict(input_text):
	return model.predict(args.prefix + input_text)
    

total_amount = len(df.index)
for index, row in df.iterrows():
    text = row['source_text']
    if pd.isnull(row['target_text']):
        converted_text = predict(text)[0]
        df.at[index, 'target_text'] = converted_text

    if index % 100 == 0:
        print("Saved at " + strftime("%H:%M:%S", localtime()) + " [" + str(index + 1) + "/" + str(total_amount) + "]")
        df.to_csv(args.csv_path)

#print(args.separator.join(to_return))
