import pandas as pd

# Load the CSV file
file_path = 'springxd.csv'  # Update with the correct path to your CSV file
data = pd.read_csv(file_path)

# Remove rows without a description
data_cleaned = data.dropna(subset=['description'])

# Create a numbered list of descriptions
numbered_descriptions = "\n".join([f"R{i+1}. {desc}" for i, desc in enumerate(data_cleaned['description'])])

# Save to a new text file
output_file_path = 'numbered_descriptions_springxd.txt'
with open(output_file_path, 'w') as file:
    file.write(numbered_descriptions)

print(f"Numbered descriptions have been saved to {output_file_path}")
