"use client"
import { Textarea } from "@nextui-org/input";
import { Button } from "@nextui-org/button";
import useSWR from 'swr'
import { useState } from 'react'
import { Spacer } from "@nextui-org/react";

export default function ScoreForm() {
const [value, setValue] = useState("");
  return (
  <>    
    <div className="flex flex-row  items-center justify-center"><Textarea
      label="Score link"
      placeholder="Enter score link"
      className="flex-auto"
      isRequired
      value={value}
      size="sm"
      maxRows={1}
      minRows={1}
      onValueChange={setValue}
    />
    <Spacer x={1}/>
    <Button color="primary" className="flex-none">Add</Button></div>
  </>
  );
}
