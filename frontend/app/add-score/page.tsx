import { Metadata } from "next";
import ScoreForm from "./form";

export const metadata: Metadata = {
  title: 'Add score'
};

export default async function AddScorePage() {
  return (
    <>
      <ScoreForm />
    </>
  );
}
