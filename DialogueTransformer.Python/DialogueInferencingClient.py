from simplet5 import SimpleT5
import argparse
import time
import torch

parser = argparse.ArgumentParser(
      prog='DialogueInferencingClient',
      description='Inference text using large language models generated with SimpleT5'
)
parser.add_argument('path', help="The path to the large language model")
parser.add_argument('prefix', help="The prefix before the input to the large language model")

args = parser.parse_args()
use_gpu = False
if torch.cuda.is_available():
    use_gpu = True


model = SimpleT5()
model.load_model("t5", args.path, use_gpu=use_gpu)

def predict(input_text):
	return model.predict(args.prefix + input_text)
    
while True:
    input_text = input()
    print(predict(input_text)[0])
