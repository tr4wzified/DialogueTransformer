from simplet5 import SimpleT5
import argparse
   
parser = argparse.ArgumentParser(
      prog='DialogPredictor',
      description='Predicts Skyrim dialogue using large language models generated with SimpleT5'
)
parser.add_argument('path', help="The path to the large language model")
parser.add_argument('prefix', help="The prefix before the input to the large language model")
args = parser.parse_args()


model = SimpleT5()
model.load_model("t5", args.path, use_gpu=False)

def predict(input_text):
	return model.predict(args.prefix + input_text)
    
while True:
    input_text = input()
    print(predict(input_text))
