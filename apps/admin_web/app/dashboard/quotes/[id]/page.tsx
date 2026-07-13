import { QuoteDetailManager } from "./quote-detail-manager";
export default async function QuoteDetailsPage({params}:PageProps<"/dashboard/quotes/[id]">){const {id}=await params;return <QuoteDetailManager id={id}/>}
