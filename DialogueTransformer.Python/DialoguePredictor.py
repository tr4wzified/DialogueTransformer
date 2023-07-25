from simplet5 import SimpleT5
import argparse
import time
import torch

parser = argparse.ArgumentParser(
      prog='DialogPredictor',
      description='Predicts Skyrim dialogue using large language models generated with SimpleT5'
)
parser.add_argument('path', help="The path to the large language model")
parser.add_argument('prefix', help="The prefix before the input to the large language model")
parser.add_argument('separator', help="Separator for splitting the input string")

args = parser.parse_args()
use_gpu = False
if torch.cuda.is_available():
    use_gpu = True


model = SimpleT5()
model.load_model("t5", args.path, use_gpu=use_gpu)

def predict(input_text):
	return model.predict(args.prefix + input_text)
    

input_text = input()
to_return = []
split_text = input_text.split(args.separator);
for text in split_text:
    prediction = predict(text)[0]
    to_return.append(prediction)
print(args.separator.join(to_return))
