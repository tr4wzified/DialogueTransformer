from simplet5 import SimpleT5
import argparse
import time

parser = argparse.ArgumentParser(
      prog='DialogPredictor',
      description='Predicts Skyrim dialogue using large language models generated with SimpleT5'
)
parser.add_argument('path', help="The path to the large language model")
parser.add_argument('prefix', help="The prefix before the input to the large language model")
parser.add_argument('separator', help="Separator for splitting the input string")

args = parser.parse_args()

model = SimpleT5()
model.load_model("t5", args.path, use_gpu=False)

def predict(input_text):
	return model.predict(args.prefix + input_text)
    

input_text = input()
to_return = []
start = time.perf_counter()
split_text = input_text.split(args.separator);
for text in input_text.split(args.separator):
    prediction = predict(text)[0]
    to_return.append(prediction)
end = time.perf_counter()
print(args.separator.join(to_return))
